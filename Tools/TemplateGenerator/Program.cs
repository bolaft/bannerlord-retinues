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
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        var templatesDir = Path.Combine(root, "Templates");
        var templatePath = Path.Combine(templatesDir, "ClanScreen_TroopsPanel.xml.sbn");
        var partialsDir = Path.Combine(templatesDir, "Partials");
        var outputPath = Path.Combine(root, "GUI", "PrefabExtensions", "ClanScreen", "ClanScreen_TroopsPanel.xml");

        var context = new TemplateContext
        {
            TemplateLoader = new LocalFileTemplateLoader(partialsDir),
            NewLine = "\n"
        };

        context.MemberRenamer = m => m.Name; // Keep property names as-is

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
