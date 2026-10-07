using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Metadata-only comparison: never loads or executes game assemblies in this process.
internal static class Program
{
    private static bool IsGame(string name) => name.StartsWith("TaleWorlds.", StringComparison.Ordinal)
        || name.StartsWith("SandBox", StringComparison.Ordinal) || name.StartsWith("NavalDLC", StringComparison.Ordinal);

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 4 || (args[0] != "diff" && args[0] != "refs"))
                throw new ArgumentException("Usage: GameAssemblyAudit.exe diff <old-dir> <installed-dir> <report.xml> OR refs <mod.dll> <installed-dir> <report.xml>");
            var report = args[0] == "diff" ? Diff(args[1], args[2]) : References(args[1], args[2]);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3])));
            new XDocument(report).Save(args[3]);
            Console.WriteLine(report.Name + ": " + string.Join(", ", report.Attributes().Select(a => a.ToString())));
            return report.Descendants("unresolved").Any() || report.Descendants("error").Any() ? 1 : 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    private static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;
            foreach (var nested in Types(type.NestedTypes)) yield return nested;
        }
    }

    private static string MethodKey(MethodReference m) => m.FullName + "|arity=" + m.GenericParameters.Count
        + "|call=" + m.CallingConvention;
    private static string MethodShape(MethodDefinition m) => string.Join("|", new[] {
        m.Attributes.ToString(), m.ImplAttributes.ToString(), m.CallingConvention.ToString(),
        string.Join(",", m.Parameters.Select(p => p.Name + ":" + p.Attributes + ":" + Value(p.Constant))),
        string.Join(",", m.GenericParameters.Select(p => p.Attributes + ":" + string.Join("&", p.Constraints.Select(c => c.ConstraintType.FullName))))
    });
    private static string Value(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
    private static string TypeShape(TypeDefinition t) => t.Attributes + "|" + t.BaseType?.FullName
        + "|" + string.Join(",", t.Interfaces.Select(i => i.InterfaceType.FullName).OrderBy(n => n));
    private static string FieldShape(FieldDefinition f) => f.Attributes + "|" + Value(f.Constant) + "|" + f.Offset;

    // Compare symbolic IL operands, not metadata tokens/byte offsets that shift on recompiles.
    private static string Body(MethodDefinition m)
    {
        if (!m.HasBody) return "";
        var body = m.Body;
        string Operand(object value)
        {
            if (value is Instruction i) return "@" + body.Instructions.IndexOf(i);
            if (value is Instruction[] targets) return string.Join(",", targets.Select(t => "@" + body.Instructions.IndexOf(t)));
            if (value is MemberReference member) return member.FullName;
            if (value is VariableDefinition variable) return "v" + variable.Index;
            if (value is ParameterDefinition parameter) return "p" + parameter.Index;
            return Value(value);
        }
        return body.InitLocals + "|" + string.Join(",", body.Variables.Select(v => v.VariableType.FullName))
            + "\n" + string.Join("\n", body.Instructions.Select(i => i.OpCode.Name + " " + Operand(i.Operand)))
            + "\n" + string.Join("\n", body.ExceptionHandlers.Select(h => h.HandlerType + " " + h.CatchType?.FullName
                + " " + Operand(h.TryStart) + " " + Operand(h.TryEnd) + " " + Operand(h.HandlerStart)
                + " " + Operand(h.HandlerEnd) + " " + Operand(h.FilterStart)));
    }

    private static void Compare<T>(XElement report, string kind, IEnumerable<T> oldValues, IEnumerable<T> newValues,
        Func<T, string> key, Func<T, string> shape)
    {
        var oldMap = oldValues.ToDictionary(key, StringComparer.Ordinal);
        var newMap = newValues.ToDictionary(key, StringComparer.Ordinal);
        foreach (var pair in oldMap)
        {
            if (!newMap.TryGetValue(pair.Key, out var next))
                report.Add(new XElement("removed", new XAttribute("kind", kind), new XAttribute("member", pair.Key)));
            else if (shape(pair.Value) != shape(next))
                report.Add(new XElement("changed", new XAttribute("kind", kind), new XAttribute("member", pair.Key),
                    new XElement("before", shape(pair.Value)), new XElement("after", shape(next))));
        }
        foreach (var pair in newMap.Where(p => !oldMap.ContainsKey(p.Key)))
            report.Add(new XElement("added", new XAttribute("kind", kind), new XAttribute("member", pair.Key)));
    }

    private static XElement Diff(string oldRoot, string currentRoot)
    {
        var report = new XElement("assemblyDiff");
        foreach (var oldPath in Directory.GetFiles(oldRoot, "*.dll").Where(p => IsGame(Path.GetFileNameWithoutExtension(p))).OrderBy(p => p))
        {
            var currentPath = Path.Combine(currentRoot, Path.GetFileName(oldPath));
            if (!File.Exists(currentPath))
            {
                // Some reference folders also hold native DLLs. Require only managed modules.
                try { using (ModuleDefinition.ReadModule(oldPath)) report.Add(new XElement("error", "Missing installed assembly: " + currentPath)); }
                catch (BadImageFormatException) { }
                continue;
            }
            ModuleDefinition oldModule;
            try { oldModule = ModuleDefinition.ReadModule(oldPath); }
            catch (BadImageFormatException) { continue; }
            using (oldModule)
            using (var current = ModuleDefinition.ReadModule(currentPath))
            {
                var assembly = new XElement("assembly", new XAttribute("name", oldModule.Name),
                    new XAttribute("oldVersion", oldModule.Assembly.Name.Version),
                    new XAttribute("newVersion", current.Assembly.Name.Version));
                var before = Types(oldModule.Types).ToList();
                var after = Types(current.Types).ToList();
                Compare(assembly, "type", before, after, t => t.FullName, TypeShape);
                Compare(assembly, "field", before.SelectMany(t => t.Fields), after.SelectMany(t => t.Fields), f => f.FullName, FieldShape);
                Compare(assembly, "method", before.SelectMany(t => t.Methods), after.SelectMany(t => t.Methods), MethodKey, MethodShape);
                var methods = after.SelectMany(t => t.Methods).ToDictionary(MethodKey);
                foreach (var method in before.SelectMany(t => t.Methods))
                    if (methods.TryGetValue(MethodKey(method), out var next) && Body(method) != Body(next))
                        assembly.Add(new XElement("bodyChanged", new XAttribute("member", method.FullName)));
                report.Add(assembly);
            }
        }
        foreach (var kind in new[] { "assembly", "removed", "added", "changed", "bodyChanged", "error" })
            report.SetAttributeValue(kind, report.Descendants(kind).Count());
        return report;
    }

    private static XElement References(string modulePath, string installedRoot)
    {
        using (var resolver = new DefaultAssemblyResolver())
        {
            // Do not fall back to any reference-folder DLLs when checking game members.
            foreach (string dir in resolver.GetSearchDirectories()) resolver.RemoveSearchDirectory(dir);
            resolver.AddSearchDirectory(installedRoot);
            resolver.AddSearchDirectory(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
            using (var module = ModuleDefinition.ReadModule(modulePath, new ReaderParameters { AssemblyResolver = resolver }))
            {
                var report = new XElement("referenceCheck", new XAttribute("module", Path.GetFullPath(modulePath)));
                bool GameType(TypeReference t)
                {
                    while (t is TypeSpecification spec) t = spec.ElementType;
                    return t.Scope is AssemblyNameReference assembly && IsGame(assembly.Name);
                }
                int checkedTypes = 0, checkedMembers = 0;
                foreach (var type in module.GetTypeReferences().Where(GameType))
                {
                    checkedTypes++;
                    try { if (type.Resolve() == null) report.Add(new XElement("unresolved", new XAttribute("kind", "type"), type.FullName)); }
                    catch (Exception e) { report.Add(new XElement("unresolved", new XAttribute("kind", "type"), type.FullName + ": " + e.Message)); }
                }
                foreach (var member in module.GetMemberReferences().Where(m => GameType(m.DeclaringType)))
                {
                    checkedMembers++;
                    try
                    {
                        object resolved = member is MethodReference method ? (object)method.Resolve()
                            : member is FieldReference field ? field.Resolve() : null;
                        if (resolved == null) report.Add(new XElement("unresolved", new XAttribute("kind", "member"), member.FullName));
                        else report.Add(new XElement("resolved", new XAttribute("member", member.FullName)));
                    }
                    catch (Exception e) { report.Add(new XElement("unresolved", new XAttribute("kind", "member"), member.FullName + ": " + e.Message)); }
                }
                report.SetAttributeValue("types", checkedTypes);
                report.SetAttributeValue("members", checkedMembers);
                report.SetAttributeValue("unresolved", report.Elements("unresolved").Count());
                return report;
            }
        }
    }
}
