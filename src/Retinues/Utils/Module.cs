using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Retinues.Utils
{
    /// <summary>
    /// Utility for querying Bannerlord modules and their metadata.
    /// Provides access to active modules, their names, versions, and official status.
    /// </summary>
    [SafeClass]
    public class ModuleChecker
    {
        public const string UnknownVersionString = "unknown";

        /// <summary>
        /// Represents a module entry with metadata parsed from SubModule.xml.
        /// </summary>
        public sealed class ModuleEntry
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Version { get; set; }
            public string Path { get; set; }
            public bool IsOfficial { get; set; }

            public override string ToString() =>
                $"{Id} [{Version}] - {Name}" + (IsOfficial ? " (official)" : "");
        }

        /// <summary>
        /// Gets a module entry by its ID, or null if not found.
        /// </summary>
        public static ModuleEntry GetModule(string id)
        {
            var modules = GetActiveModules();
            foreach (var mod in modules)
            {
                if (mod.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                    return mod;
            }
            return null;
        }

        /// <summary>
        /// Checks if a module with the given ID is active.
        /// </summary>
        public static bool IsLoaded(string id) => GetModule(id) != null;

        /// <summary>
        /// Checks if any of the given module IDs is active. Pass multiple IDs to test several modules at once.
        /// Returns true if at least one of the provided IDs is present in the active modules list.
        /// </summary>
        public static bool IsLoaded(params string[] ids)
        {
            if (ids == null || ids.Length == 0)
                return false;

            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (GetModule(id) != null)
                    return true;
            }

            return false;
        }

        private static List<ModuleEntry> _cachedActiveModules;

        /// <summary>
        /// Returns a list of all active modules, with metadata parsed from SubModule.xml.
        /// Results are cached for future calls.
        /// </summary>
        public static List<ModuleEntry> GetActiveModules()
        {
            if (_cachedActiveModules != null)
                return _cachedActiveModules;

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
                string version = UnknownVersionString;
                bool isOfficial = IsOfficialModuleId(id); // quick heuristic

                try
                {
                    if (File.Exists(submodulePath))
                    {
                        var doc = XDocument.Load(submodulePath);
                        var root = doc.Root; // <Module ...>

                        // Name
                        name =
                            (string)root?.Attribute("name")
                            ?? (root?.Element("Name")?.Value)
                            ?? name;

                        // Version - try attribute, then element patterns used by some tools
                        version =
                            (string)root?.Attribute("version")
                            ?? (string)root?.Element("Version")?.Attribute("value")
                            ?? (root?.Element("Version")?.Value)
                            ?? version;

                        // Official flag (some SubModule.xml include an Official tag/attr)
                        var officialAttr = (string)root?.Attribute("official");
                        if (!string.IsNullOrEmpty(officialAttr))
                            isOfficial = officialAttr.Equals(
                                "true",
                                StringComparison.OrdinalIgnoreCase
                            );

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

                result.Add(
                    new ModuleEntry
                    {
                        Id = id,
                        Name = name,
                        Version = version,
                        Path = modDir,
                        IsOfficial = isOfficial,
                    }
                );
            }

            // Cache for future calls
            _cachedActiveModules = result;

            return result;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
