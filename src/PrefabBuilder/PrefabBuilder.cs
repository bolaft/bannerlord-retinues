using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

/// <summary>
/// CLI tool for generating GUI prefabs from Scriban templates.
/// Loads templates and partials, renders to XML, and writes output files.
/// Now version-aware: exposes 'bl', 'dev', 'module', 'version' to templates.
/// </summary>
class PrefabBuilder
{
    static async Task<int> Main(string[] args)
    {
        // Repo root (../../../../ from build output)
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        // Defaults derived from repo layout
        var defaults = new
        {
            Root = root,
            Templates = Path.Combine(root, "tpl", "templates"),
            Partials = Path.Combine(root, "tpl", "partials"),
            Module = "Retinues.Core",
            Out = Path.Combine(root, "bin", "Modules", "Retinues.Core", "GUI"),
            // Environment can override some behavior without args
            BL = EnvInt("BL", 13),
            Config = (Environment.GetEnvironmentVariable("CONFIG") ?? "dev").Trim(),
            Version = Environment.GetEnvironmentVariable("VERSION") ?? "", // optional
        };

        // Parse CLI flags (all optional)
        var opts = ParseArgs(args);
        int bl =
            opts.TryGetValue("bl", out var blStr) && int.TryParse(blStr, out var blParsed)
                ? blParsed
                : defaults.BL;
        string config = opts.GetValueOrDefault("config", defaults.Config);
        bool dev = !string.Equals(config, "Release", StringComparison.OrdinalIgnoreCase);

        string module = opts.GetValueOrDefault("module", defaults.Module);
        string tplTemplates = opts.GetValueOrDefault("templates", defaults.Templates);
        string tplPartials = opts.GetValueOrDefault("partials", defaults.Partials);
        string outGui = opts.GetValueOrDefault("out", defaults.Out);
        string version = opts.GetValueOrDefault("version", defaults.Version);

        Console.WriteLine("Config:");
        Console.WriteLine($"  Root      : {Os(defaults.Root)}");
        Console.WriteLine($"  Templates : {Os(tplTemplates)}");
        Console.WriteLine($"  Partials  : {Os(tplPartials)}");
        Console.WriteLine($"  Out       : {Os(outGui)}");
        Console.WriteLine($"  bl        : {bl}");
        Console.WriteLine($"  config    : {config} (dev={dev})");
        if (!string.IsNullOrWhiteSpace(version))
            Console.WriteLine($"  version   : {version}");
        Console.WriteLine($"  module    : {module}");
        Console.WriteLine();

        int files = await RenderAll(
            tplTemplates,
            tplPartials,
            outGui,
            globals: new Dictionary<string, object?>
            {
                ["bl"] = bl, // int  -> 12 or 13
                ["dev"] = dev, // bool -> true if not Release
                ["module"] = module, // string
                ["version"] = version, // string | empty if not provided
            }
        );

        Console.WriteLine($"[PrefabBuilder] Generated {files} file(s).");
        return 0;
    }

    static async Task<int> RenderAll(
        string templatesDir,
        string partialsDir,
        string outGui,
        IDictionary<string, object?> globals
    )
    {
        if (!Directory.Exists(templatesDir))
        {
            Console.WriteLine($"[PrefabBuilder] No templates dir: {templatesDir}");
            return 0;
        }

        // Prepare Scriban context + globals
        var context = new TemplateContext
        {
            TemplateLoader = new LocalFileTemplateLoader(partialsDir),
            NewLine = "\n",
            MemberRenamer = m => m.Name, // keep member names as-is
        };

        var scriptGlobals = new ScriptObject();
        // Import each global (bl/dev/module/version) into the template context
        foreach (var kv in globals)
            scriptGlobals.Import(new Dictionary<string, object?> { [kv.Key] = kv.Value });
        context.PushGlobal(scriptGlobals);

        var templates = Directory.GetFiles(templatesDir, "*.sbn", SearchOption.AllDirectories);
        int count = 0;

        foreach (var tpl in templates)
        {
            var rel = Path.GetRelativePath(templatesDir, tpl);
            var outRel = Path.ChangeExtension(rel, ".xml");
            var subdir = rel.Contains("Clan", StringComparison.OrdinalIgnoreCase)
                ? "ClanScreen"
                : string.Empty;

            var outPath = Path.Combine(
                outGui,
                "PrefabExtensions",
                string.IsNullOrEmpty(subdir) ? string.Empty : subdir,
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

    // ---- helpers ----

    static int EnvInt(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var v) ? v : fallback;

    static string Os(string p) => Path.GetFullPath(p)
    .Replace('/', Path.DirectorySeparatorChar)
    .Replace('\\', Path.DirectorySeparatorChar);

    static Dictionary<string, string> ParseArgs(string[] args)
    {
        // Very small argv parser: supports --key value OR --key=value OR -k value
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a.StartsWith("--"))
            {
                var eq = a.IndexOf('=');
                if (eq > 2)
                {
                    var k = a[2..eq];
                    var v = a[(eq + 1)..];
                    map[k] = v;
                }
                else
                {
                    var k = a[2..];
                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                    {
                        map[k] = args[++i];
                    }
                    else
                    {
                        map[k] = "true";
                    }
                }
            }
            else if (a.StartsWith("-") && a.Length > 1)
            {
                // short forms: -o, -t, -p, -m, -v, etc.
                string? key = a switch
                {
                    "-o" => "out",
                    "-t" => "templates",
                    "-p" => "partials",
                    "-b" => "bl",
                    "-c" => "config",
                    "-m" => "module",
                    "-v" => "version",
                    _ => null,
                };
                if (key != null)
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        map[key] = args[++i];
                    else
                        map[key] = "true";
                }
            }
        }
        return map;
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
