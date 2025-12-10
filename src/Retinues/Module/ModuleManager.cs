using System;
using System.Collections.Generic;
using System.IO;
using Retinues.Utilities;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace Retinues.Module
{
    /// <summary>
    /// Utility for querying Bannerlord modules and their metadata.
    /// Provides access to active modules, their names, versions, and official status.
    /// </summary>
    [SafeClass]
    public static class ModuleManager
    {
        public const string UnknownVersionString = "unknown";

        /// <summary>
        /// Represents a module entry with metadata parsed from SubModule.xml.
        /// </summary>
        public sealed class ModuleInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }

            /// <summary>
            /// Raw ApplicationVersion from TaleWorlds.ModuleManager.ModuleInfo.
            /// </summary>
            public ApplicationVersion AppVersion { get; set; }

            /// <summary>
            /// String representation used by logs and save data.
            /// Falls back to 'unknown' if AppVersion is Empty/invalid.
            /// </summary>
            public string Version =>
                AppVersion == ApplicationVersion.Empty
                    ? UnknownVersionString
                    : AppVersion.ToString();

            public string Path { get; set; }
            public bool IsOfficial { get; set; }

            public override string ToString()
            {
                return string.Format(
                    "{0} [{1}] - {2}{3}",
                    Id,
                    Version,
                    Name,
                    IsOfficial ? " (official)" : string.Empty
                );
            }
        }

        private static List<ModuleInfo> _cachedActiveModules;

        /// <summary>
        /// Returns a list of all active modules, with metadata parsed from SubModule.xml.
        /// Results are cached for future calls.
        /// </summary>
        public static IReadOnlyList<ModuleInfo> GetActiveModules()
        {
            if (_cachedActiveModules != null)
                return _cachedActiveModules;

            var result = new List<ModuleInfo>();

            string[] ordered;
            try
            {
                ordered = TaleWorlds.Engine.Utilities.GetModulesNames();
            }
            catch
            {
                ordered = [];
            }

            foreach (var id in ordered)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                string name = id;
                ApplicationVersion appVersion = ApplicationVersion.Empty;
                bool isOfficial = IsOfficialModuleId(id);
                string modDir = null;

                try
                {
                    var info = ModuleHelper.GetModuleInfo(id);
                    if (info != null)
                    {
                        name = info.Name ?? name;
                        isOfficial = info.IsOfficial;
                        appVersion = info.Version; // from SubModule.xml
                        modDir = info.FolderPath; // root path (Modules or workshop)
                    }
                }
                catch
                {
                    // Ignore and fall back to manual path
                }

                if (string.IsNullOrEmpty(modDir))
                {
                    var modulesRoot = Path.Combine(BasePath.Name, "Modules");
                    modDir = Path.Combine(modulesRoot, id);
                }

                result.Add(
                    new ModuleInfo
                    {
                        Id = id,
                        Name = name,
                        AppVersion = appVersion,
                        Path = modDir,
                        IsOfficial = isOfficial,
                    }
                );
            }

            _cachedActiveModules = result;
            return _cachedActiveModules;
        }

        /// <summary>
        /// Clears the cached module list. Call if you expect the active modules to change.
        /// </summary>
        public static void ClearCache()
        {
            _cachedActiveModules = null;
        }

        /// <summary>
        /// Gets a module entry by its ID, or null if not found.
        /// </summary>
        public static ModuleInfo GetModule(string id)
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
        public static bool IsLoaded(string id)
        {
            return GetModule(id) != null;
        }

        /// <summary>
        /// Checks if any of the given module IDs is active.
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

        /// <summary>
        /// Initializes the module manager and optionally logs module info.
        /// </summary>
        public static void Initialize(bool logModules = true)
        {
            var modules = GetActiveModules();

            if (!logModules)
                return;

            try
            {
                var appVersion = ApplicationVersion.FromParametersFile();
                Log.Info(
                    string.Format(
                        "Bannerlord version: {0}.{1}.{2}",
                        appVersion.Major,
                        appVersion.Minor,
                        appVersion.Revision
                    )
                );
            }
            catch
            {
                Log.Info("Bannerlord version: <unknown>");
            }

            Log.Info("Active modules:");
            foreach (var mod in modules)
            {
                Log.Info(
                    string.Format(
                        "    {0} {1} {2}",
                        mod.IsOfficial ? "[Official] " : "[Community]",
                        mod.Id,
                        mod.Version
                    )
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool IsOfficialModuleId(string id)
        {
            switch (id)
            {
                case "Native":
                case "SandboxCore":
                case "Sandbox":
                case "StoryMode":
                case "CustomBattle":
                    return true;
                default:
                    return false;
            }
        }
    }
}
