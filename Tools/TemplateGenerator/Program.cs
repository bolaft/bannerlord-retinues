using System;
using System.IO;
using System.Threading.Tasks;
using Scriban;
using Scriban.Runtime;
using Scriban.Parsing;

class Program
{
    static async Task Main(string[] args)
    {
        // Project root detection: adjust if needed
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        // Paths
        var templatesDir = Path.Combine(root, "Templates");
        var templatePath = Path.Combine(templatesDir, "ClanScreen_TroopsPanel.xml.sbn");
        var partialsDir = Path.Combine(templatesDir, "Partials");
        var outputPath = Path.Combine(root, "GUI", "PrefabExtensions", "ClanScreen", "ClanScreen_TroopsPanel.xml");

        // --- Define skills exactly as in reference XML ---
        var skills_row1 = new[]
        {
            new { skill_id = "athletics", margin = 40, name = "Athletics" },
            new { skill_id = "riding",    margin = 40, name = "Riding" },
            new { skill_id = "onehanded", margin = 40, name = "OneHanded" },
            new { skill_id = "twohanded", margin = 0,  name = "TwoHanded" },
        };

        var skills_row2 = new[]
        {
            new { skill_id = "polearm",  margin = 40, name = "Polearm" },
            new { skill_id = "bow",      margin = 40, name = "Bow" },
            new { skill_id = "crossbow", margin = 40, name = "Crossbow" },
            new { skill_id = "throwing", margin = 0,  name = "Throwing" },
        };

        // --- Define troop sections exactly as in reference ---
        var troop_sections = new[]
        {
            new { id = "Elite",   label = "Elite",   dataSource = "CustomElite" },
            new { id = "Regular", label = "Regular", dataSource = "CustomBasic" }
        };

        var globals = new ScriptObject();
        globals["skills_row1"] = skills_row1;
        globals["skills_row2"] = skills_row2;
        globals["troop_sections"] = troop_sections;

        var context = new TemplateContext
        {
            TemplateLoader = new LocalFileTemplateLoader(partialsDir),
            NewLine = "\n"
        };

        context.MemberRenamer = m => m.Name; // Keep property names as-is
        context.PushGlobal(globals);

        if (!File.Exists(templatePath))
        {
            Console.Error.WriteLine($"Template not found: {templatePath}");
            Environment.Exit(1);
        }

        var templateText = await File.ReadAllTextAsync(templatePath);
        var template = Template.Parse(templateText, templatePath);

        if (template.HasErrors)
        {
            Console.Error.WriteLine("Template parse errors:");
            foreach (var msg in template.Messages)
                Console.Error.WriteLine("- " + msg);
            Environment.Exit(1);
        }

        string result;
        try
        {
            result = await template.RenderAsync(context);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Render failed:");
            Console.Error.WriteLine(ex);
            Environment.Exit(1);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, result);
        Console.WriteLine($"Generated: {outputPath}");
    }
}

// Loader for {{ include "file.sbn" }}
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

    // Overloads for Scriban >=5
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        GetPath(context, templateName);

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        Load(context, templatePath);

    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        LoadAsync(context, templatePath);
}
