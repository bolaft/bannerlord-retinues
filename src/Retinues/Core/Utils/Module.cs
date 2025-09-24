using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using TaleWorlds.Library;
using TaleWorlds.Engine;

namespace Retinues.Core.Utils
{
    public static class ModuleChecker
    {
        public sealed class ModuleEntry
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public string Path { get; set; }
            public bool   IsOfficial { get; set; }

            public override string ToString() =>
                $"{Id} [{Version}] - {Name}" + (IsOfficial ? " (official)" : "");
        }

        public static string GetGameVersionString()
        {
            try
            {
                var v = ApplicationVersion.FromParametersFile();
                return v.ToString() ?? "unknown";
            }
            catch { return "unknown"; }
        }

        public static List<ModuleEntry> GetActiveModules()
        {
            var result = new List<ModuleEntry>();

            // 1) Load order from the engine (official + mods)
            string[] ordered;
            try
            {
                ordered = Utilities.GetModulesNames();
            }
            catch
            {
                // Fallback in the unlikely case the API moves
                ordered = [];
            }

            // 2) Resolve each module to its SubModule.xml and parse metadata
            var modulesRoot = System.IO.Path.Combine(BasePath.Name, "Modules");
            foreach (var id in ordered)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var modDir = System.IO.Path.Combine(modulesRoot, id);
                var submodulePath = System.IO.Path.Combine(modDir, "SubModule.xml");

                string name = id;
                string version = "unknown";
                bool isOfficial = IsOfficialModuleId(id); // quick heuristic

                try
                {
                    if (File.Exists(submodulePath))
                    {
                        var doc = XDocument.Load(submodulePath);
                        var root = doc.Root; // <Module ...>

                        // Name
                        name =
                            (string)root?.Attribute("name") ??
                            (string)root?.Element("Name")?.Value ??
                            name;

                        // Version — try attribute, then element patterns used by some tools
                        version =
                            (string)root?.Attribute("version") ??
                            (string)root?.Element("Version")?.Attribute("value") ??
                            (string)root?.Element("Version")?.Value ??
                            version;

                        // Official flag (some SubModule.xml include an Official tag/attr)
                        var officialAttr = (string)root?.Attribute("official");
                        if (!string.IsNullOrEmpty(officialAttr))
                            isOfficial = officialAttr.Equals("true", StringComparison.OrdinalIgnoreCase);

                        var officialEl = root?.Element("Official");
                        if (officialEl != null)
                        {
                            var ov = (string)officialEl.Attribute("value") ?? officialEl.Value;
                            if (!string.IsNullOrEmpty(ov))
                                isOfficial = ov.Equals("true", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
                catch { }

                result.Add(new ModuleEntry
                {
                    Id = id,
                    Name = name,
                    Version = version,
                    Path = modDir,
                    IsOfficial = isOfficial
                });
            }

            return result;
        }

        private static bool IsOfficialModuleId(string id)
        {
            return id switch
            {
                "Native" or "SandboxCore" or "Sandbox" or "StoryMode" or "CustomBattle" => true,
                _ => false,
            };
        }
    }
}
