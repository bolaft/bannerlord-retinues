using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

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

            // 2) Resolve each module via ModuleHelper to support Workshop/platform mods
            foreach (var id in ordered)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                string name = id;
                string version = "unknown";
                bool isOfficial = IsOfficialModuleId(id);
                string modDir = null;

                try
                {
                    // This works for both physical Modules/* and Steam Workshop modules
                    var info = ModuleHelper.GetModuleInfo(id);
                    if (info != null)
                    {
                        name = info.Name ?? name;
                        isOfficial = info.IsOfficial;

                        // ApplicationVersion.ToString() gives e1.2.12 etc; adapt if you prefer
                        version = info.Version.ToString();

                        // FolderPath is the real root path (Modules or workshop/content/261550/...)
                        modDir = info.FolderPath;
                    }
                }
                catch
                {
                    // Ignore and fall back to manual path below
                }

                // Fallback for safety (e.g. if ModuleHelper failed for some reason)
                if (string.IsNullOrEmpty(modDir))
                {
                    var modulesRoot = System.IO.Path.Combine(BasePath.Name, "Modules");
                    modDir = System.IO.Path.Combine(modulesRoot, id);
                }

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
