using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Scriban;
using Scriban.Runtime;
using Scriban.Parsing;

class PrefabBuilder
{
    static async Task<int> Main(string[] args)
    {
        // repo root = .../ (we're under src/PrefabBuilder/bin/* when running)
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

        Console.WriteLine($"[PrefabBuilder] Root directory: {root}");

        // template roots per submod
        var tplRoot = Path.Combine(root, "tpl", "Retinues");
        var coreTpl = Path.Combine(tplRoot, "Core");
        var corePartials = Path.Combine(coreTpl, "partials");
        var coreTemplates = Path.Combine(coreTpl, "templates");

        // where generated files should go
        var outGuiCore = Path.Combine(root, "bin", "Modules", "Retinues.Core", "GUI");

        int files = 0;
        files += await RenderAll(coreTemplates, corePartials, outGuiCore);

        Console.WriteLine($"[PrefabBuilder] Generated {files} file(s).");
        return 0;
    }

    static async Task<int> RenderAll(string templatesDir, string partialsDir, string outGui)
    {
        if (!Directory.Exists(templatesDir))
            return 0;

        var context = new TemplateContext
        {
            TemplateLoader = new LocalFileTemplateLoader(partialsDir),
            NewLine = "\n"
        };
        context.MemberRenamer = m => m.Name;

        // Find all .sbn under /templates (recursively). Output is same structure, but .xml
        var templates = Directory.GetFiles(templatesDir, "*.sbn", SearchOption.AllDirectories);
        int count = 0;

        foreach (var tpl in templates)
        {
            var rel = Path.GetRelativePath(templatesDir, tpl);         // e.g. "ClanScreen_TroopsTab.sbn" or "Constants\X.sbn"
            var outRel = Path.ChangeExtension(rel, ".xml");            // -> *.xml
            var outPath = Path.Combine(outGui, "PrefabExtensions", rel.Contains("Clan") ? "ClanScreen" : "", outRel);

            // Better: if your templates already contain subfolders like PrefabExtensions\ClanScreen\..., just mirror them:
            // var outRel = rel.Replace(".sbn", ".xml");
            // var outPath = Path.Combine(outGui, outRel);

            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

            var text = await File.ReadAllTextAsync(tpl);
            var parsed = Template.Parse(text, tpl);
            if (parsed.HasErrors)
            {
                Console.Error.WriteLine($"[PrefabBuilder] Parse errors in {tpl}:");
                foreach (var m in parsed.Messages) Console.Error.WriteLine("  - " + m);
                continue;
            }

            try
            {
                var result = await parsed.RenderAsync(context);
                await File.WriteAllTextAsync(outPath, result);
                Console.WriteLine($"[PrefabBuilder] Generated: {outPath}");
                count++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PrefabBuilder] Render failed for {tpl}:\n{ex}");
            }
        }

        return count;
    }
}

class LocalFileTemplateLoader : ITemplateLoader
{
    private readonly string _baseDir;
    public LocalFileTemplateLoader(string baseDir) => _baseDir = baseDir;

    public string GetPath(TemplateContext context, string templateName) =>
        Path.Combine(_baseDir, templateName);
    public string Load(TemplateContext context, string templatePath) =>
        File.ReadAllText(templatePath);
    public ValueTask<string> LoadAsync(TemplateContext context, string templatePath) =>
        new(File.ReadAllTextAsync(templatePath));

    // For Scriban >= 5 overloads:
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        GetPath(context, templateName);
    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        Load(context, templatePath);
    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        LoadAsync(context, templatePath);
}
