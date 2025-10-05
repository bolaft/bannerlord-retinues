using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

/// <summary>
/// CLI tool for generating GUI prefabs from Scriban templates.
/// Loads templates and partials, renders to XML, and writes output files.
/// </summary>
class PrefabBuilder
{
    static async Task<int> Main(string[] args)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        Console.WriteLine($"[PrefabBuilder] Root directory: {root}");

        // template roots per submod
        var tplRoot = Path.Combine(root, "tpl");
        var tplPartials = Path.Combine(tplRoot, "partials");
        var tplTemplates = Path.Combine(tplRoot, "templates");

        // where generated files should go
        var outGuiCore = Path.Combine(root, "bin", "Modules", "Retinues.Core", "GUI");

        Console.WriteLine($"[PrefabBuilder] Templates: {tplTemplates}");
        Console.WriteLine($"[PrefabBuilder] Partials: {tplPartials}");

        int files = 0;
        files += await RenderAll(tplTemplates, tplPartials, outGuiCore);

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
            NewLine = "\n",
            MemberRenamer = m => m.Name,
        };

        var templates = Directory.GetFiles(templatesDir, "*.sbn", SearchOption.AllDirectories);
        int count = 0;

        foreach (var tpl in templates)
        {
            var rel = Path.GetRelativePath(templatesDir, tpl);
            var outRel = Path.ChangeExtension(rel, ".xml");
            var outPath = Path.Combine(
                outGui,
                "PrefabExtensions",
                rel.Contains("Clan") ? "ClanScreen" : "",
                outRel
            );

            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

            var text = await File.ReadAllTextAsync(tpl);
            var parsed = Template.Parse(text, tpl);
            if (parsed.HasErrors)
            {
                Console.Error.WriteLine($"[PrefabBuilder] Parse errors in {tpl}:");
                foreach (var m in parsed.Messages)
                    Console.Error.WriteLine("  - " + m);
                continue;
            }

            try
            {
                var result = await parsed.RenderAsync(context);
                await File.WriteAllTextAsync(outPath, result);
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

/// <summary>
/// Template loader for Scriban that loads partials from a local directory.
/// </summary>
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

    public ValueTask<string> LoadAsync(
        TemplateContext context,
        SourceSpan callerSpan,
        string templatePath
    ) => LoadAsync(context, templatePath);
}
