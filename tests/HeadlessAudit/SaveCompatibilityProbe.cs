using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;

// Runs only in a separate headless process. The baseline must be exported by an older binary;
// these XML reports are managed compatibility fixtures, never Bannerlord campaign save files.
internal static class SaveCompatibilityProbe
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Run(Assembly module, string branch, string path, string baselinePath)
    {
        var root = CaptureSchema(module, branch);
        root.SetAttributeValue("moduleVersion", module.GetName().Version);
        if (branch == "v2") ExportV2Payload(module, root);
        if (baselinePath != null)
        {
            var baseline = XDocument.Load(baselinePath).Root;
            CompareSchema(baseline, root);
            if (branch == "v2" && baseline.Element("model") != null)
                LoadPriorV2Payload(module, baseline);
            Console.WriteLine("PASS all prior registered classes, field contracts and containers remain readable");
        }
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        new XDocument(root).Save(path);
        Console.WriteLine($"Exported {root.Elements("class").Count()} registered classes, {root.Descendants("field").Count()} fields, {root.Elements("container").Count()} containers: {path}");
    }

    private static XElement CaptureSchema(Assembly module, string branch)
    {
        var engine = Assembly.Load("TaleWorlds.SaveSystem");
        var baseDefiner = engine.GetType("TaleWorlds.SaveSystem.SaveableTypeDefiner", true);
        var contextType = engine.GetType("TaleWorlds.SaveSystem.Definition.DefinitionContext", true);
        var context = Activator.CreateInstance(contextType);
        // An isolated engine definition context; no global save registry or Harmony interception.
        var basic = Activator.CreateInstance(engine.GetType("TaleWorlds.SaveSystem.SaveableBasicTypeDefiner", true));
        baseDefiner.GetMethod("Initialize", Instance).Invoke(basic, new[] { context });
        basic.GetType().GetMethod("DefineBasicTypes", Instance).Invoke(basic, null);
        // Include V2's behavior registry as well as its legacy reader. At present the behavior
        // registry adds no classes/containers, but future contributions must enter this check.
        var definers = module.GetTypes().Where(t => !t.IsAbstract && baseDefiner.IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal).Select(Activator.CreateInstance).ToList();
        Require(definers.Count > 0, "No save type definers found in the module.");
        foreach (var definer in definers)
        {
            baseDefiner.GetMethod("Initialize", Instance).Invoke(definer, new[] { context });
            definer.GetType().GetMethod("DefineClassTypes", Instance).Invoke(definer, null);
            definer.GetType().GetMethod("DefineContainerDefinitions", Instance).Invoke(definer, null);
        }
        var errors = ((IEnumerable)contextType.GetProperty("Errors").GetValue(context)).Cast<string>().ToList();
        Require(errors.Count == 0, "Engine rejected save registrations: " + string.Join("; ", errors));
        var classes = (IDictionary)contextType.GetField("_classDefinitions", Instance).GetValue(context);
        var ids = new Dictionary<Type, string>();
        foreach (DictionaryEntry entry in classes) ids[(Type)entry.Key] = SaveId(entry.Value);
        var result = new XElement("saveContract");
        foreach (var pair in ids.OrderBy(p => p.Value, StringComparer.Ordinal))
        {
            var element = new XElement("class", new XAttribute("id", pair.Value), new XAttribute("name", pair.Key.Name));
            foreach (var field in pair.Key.GetFields(Instance))
            {
                var attribute = field.GetCustomAttributesData().SingleOrDefault(a => a.AttributeType.Name == "SaveableFieldAttribute");
                if (attribute == null) continue;
                element.Add(new XElement("field", new XAttribute("id", attribute.ConstructorArguments[0].Value),
                    new XAttribute("name", field.Name), new XAttribute("type", WireType(field.FieldType, ids))));
            }
            Require(element.Elements("field").Select(f => (string)f.Attribute("id")).Distinct().Count() == element.Elements("field").Count(),
                "Duplicate field ID in " + pair.Key.Name);
            result.Add(element);
        }
        var containers = (IDictionary)contextType.GetField("_containerDefinitions", Instance).GetValue(context);
        foreach (DictionaryEntry entry in containers)
            result.Add(new XElement("container", new XAttribute("id", SaveId(entry.Value)),
                new XAttribute("type", WireType((Type)entry.Key, ids))));
        return result;
    }

    private static string SaveId(object definition)
    {
        var id = definition.GetType().GetProperty("SaveId").GetValue(definition);
        return (string)id.GetType().GetMethod("GetStringId").Invoke(id, null);
    }

    private static string WireType(Type type, Dictionary<Type, string> ids)
    {
        if (ids.TryGetValue(type, out var id)) return "class:" + id;
        if (type.IsArray) return WireType(type.GetElementType(), ids) + "[]";
        if (type.IsGenericType)
            return type.GetGenericTypeDefinition().FullName + "[" + string.Join(",", type.GetGenericArguments().Select(t => WireType(t, ids))) + "]";
        return type.FullName;
    }

    private static void CompareSchema(XElement before, XElement after)
    {
        Require(before?.Name == "saveContract" && before.Elements("class").Any(), "The baseline must contain an exported save contract.");
        foreach (var oldClass in before.Elements("class"))
        {
            var current = after.Elements("class").SingleOrDefault(c => (string)c.Attribute("id") == (string)oldClass.Attribute("id"));
            Require(current != null, "Removed prior class ID: " + oldClass);
            Require((string)current.Attribute("name") == (string)oldClass.Attribute("name"), "Reused class ID: " + oldClass);
            foreach (var field in oldClass.Elements("field"))
                Require(current.Elements("field").Any(f => XNode.DeepEquals(f, field)), "Removed or changed prior field: " + oldClass.Attribute("name") + " " + field);
        }
        foreach (var container in before.Elements("container"))
            Require(after.Elements("container").Any(c => XNode.DeepEquals(c, container)), "Removed or changed prior container: " + container);
    }

    private static Type _modelType;
    private static object CreateModel(Assembly module)
    {
        if (_modelType == null)
        {
            var parent = module.GetType("Retinues.Framework.Model.MBase`1", true).MakeGenericType(typeof(object));
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("CompatibilityFixture"), AssemblyBuilderAccess.Run);
            var type = assembly.DefineDynamicModule("Fixture").DefineType("CompatibilityModel", TypeAttributes.Public, parent);
            var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes).GetILGenerator();
            ctor.Emit(OpCodes.Ldarg_0);
            ctor.Emit(OpCodes.Newobj, typeof(object).GetConstructor(Type.EmptyTypes));
            ctor.Emit(OpCodes.Call, parent.GetConstructor(Instance, null, new[] { typeof(object) }, null));
            ctor.Emit(OpCodes.Ret);
            _modelType = type.CreateType();
        }
        return Activator.CreateInstance(_modelType);
    }

    private static void Attribute<T>(object model, string name, T value)
    {
        var factory = model.GetType().GetMethods(Instance).Single(m => m.Name == "Attribute" && m.IsGenericMethodDefinition
            && m.GetParameters()[0].Name == "initialValue").MakeGenericMethod(typeof(T));
        var args = factory.GetParameters().Select(p => p.DefaultValue).ToArray();
        args[0] = value;
        args[Array.FindIndex(factory.GetParameters(), p => p.Name == "name")] = name;
        factory.Invoke(model, args);
    }

    private static void Populate(object model, bool defaults)
    {
        Attribute(model, "NameAttribute", defaults ? "" : "Garde <&> Élite 日本");
        Attribute(model, "SkillPointsAttribute", defaults ? 0 : 47);
        Attribute(model, "ItemStagingProgress", defaults ? 0f : 3.125f);
        Attribute(model, "ItemsStaging", defaults ? new List<string>() : new List<string> { "0|crafted_item_old", "1|" });
        Attribute(model, "Stocks", defaults ? new Dictionary<string, int>() : new Dictionary<string, int> { ["crafted_item_old"] = 4 });
        Attribute(model, "Enabled", !defaults);
        Attribute(model, "Fraction", defaults ? 0d : 0.875d);
    }

    private static string Serialize(object model, bool all) => (string)model.GetType().GetMethod(all ? "SerializeAll" : "Serialize").Invoke(model, null);

    private static void ExportV2Payload(Assembly module, XElement root)
    {
        var model = CreateModel(module);
        Populate(model, false);
        string payload = Serialize(model, true);
        Require(!string.IsNullOrEmpty(payload), "Prior writer produced empty model data.");
        root.Add(new XElement("model", payload));
        var persistence = module.GetType("Retinues.Framework.Model.Persistence.MPersistenceBehavior", true);
        root.Add(new XElement("packed", (string)persistence.GetMethod("PackToBase64Gzip", Static).Invoke(null, new object[] { payload })));
        var behaviorType = module.GetType("Retinues.Behaviors.Experience.SharedSkillPoolBehavior", true);
        var behavior = Activator.CreateInstance(behaviorType);
        behaviorType.GetField("_sharedSkillPoints", Instance).SetValue(behavior, 7);
        behaviorType.GetField("_sharedSkillPointsExperience", Instance).SetValue(behavior, 33000);
        var data = new Dictionary<string, object>();
        behaviorType.GetMethod("SyncData").Invoke(behavior, new object[] { new Store(data, true) });
        root.Add(new XElement("sharedPool", data.Select(p => new XElement("entry", new XAttribute("key", p.Key),
            new XAttribute("type", p.Value.GetType().FullName), Convert.ToString(p.Value, CultureInfo.InvariantCulture)))));
    }

    private static void LoadPriorV2Payload(Assembly module, XElement old)
    {
        var persistence = module.GetType("Retinues.Framework.Model.Persistence.MPersistenceBehavior", true);
        string payload = (string)persistence.GetMethod("UnpackFromBase64Gzip", Static).Invoke(null, new object[] { old.Element("packed").Value });
        Require(payload == old.Element("model").Value, "New reader cannot unpack prior compressed payload.");
        foreach (string culture in new[] { "en-US", "fr-FR", "tr-TR" })
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            for (int generation = 0; generation < 4; generation++)
            {
                var model = CreateModel(module);
                Populate(model, true);
                model.GetType().GetMethod("Deserialize").Invoke(model, new object[] { payload });
                Require(XNode.DeepEquals(XElement.Parse(old.Element("model").Value), XElement.Parse(Serialize(model, true))),
                    "Prior values changed after loading under " + culture);
                payload = Serialize(model, false);
                Require(!string.IsNullOrWhiteSpace(payload), "Loaded values disappeared from the next save.");
            }
        }
        var entries = old.Element("sharedPool").Elements("entry");
        var data = entries.ToDictionary(e => (string)e.Attribute("key"), e =>
            Convert.ChangeType(e.Value, Type.GetType((string)e.Attribute("type"), true), CultureInfo.InvariantCulture));
        var behaviorType = module.GetType("Retinues.Behaviors.Experience.SharedSkillPoolBehavior", true);
        var behavior = Activator.CreateInstance(behaviorType);
        behaviorType.GetMethod("SyncData").Invoke(behavior, new object[] { new Store(data, false) });
        Require((int)behaviorType.GetField("_sharedSkillPoints", Instance).GetValue(behavior) == 7, "Existing points were lost.");
        Require((int)behaviorType.GetField("_sharedSkillPointsExperience", Instance).GetValue(behavior) == 33000, "Existing raw XP was lost.");
        // With no registered campaign troops, conversion must defer without discarding the XP.
        behaviorType.GetMethod("MigrateLegacyExperience", Instance)?.Invoke(behavior, null);
        var roundTrip = new Dictionary<string, object>();
        behaviorType.GetMethod("SyncData").Invoke(behavior, new object[] { new Store(roundTrip, true) });
        foreach (var entry in data) Require(roundTrip.TryGetValue(entry.Key, out var value) && Equals(entry.Value, value), "Old key changed during deferred conversion: " + entry.Key);
        Console.WriteLine("PASS prior V2 writer -> current reader -> four resaves across three cultures; pending shared XP survives load/resave");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private sealed class Store(Dictionary<string, object> values, bool saving) : IDataStore
    {
        public bool IsSaving => saving;
        public bool IsLoading => !saving;
        public bool SyncData<T>(string key, ref T data)
        {
            if (saving) values[key] = data;
            else if (values.TryGetValue(key, out var value)) data = (T)value;
            else return false;
            return true;
        }
    }
}
