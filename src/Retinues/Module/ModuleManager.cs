using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
        public const string SelfModuleId = "Retinues";

        public sealed class ModuleInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public bool IsLoaded => AppVersion != ApplicationVersion.Empty;

            public ApplicationVersion AppVersion { get; set; }

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

        private static Dictionary<string, string> _cachedExpectedDependencyVersions;
        private static string _cachedExpectedVersionsOwnerPath;

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
                        appVersion = info.Version;
                        modDir = info.FolderPath;
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

        public static void ClearCache()
        {
            _cachedActiveModules = null;
            _cachedExpectedDependencyVersions = null;
            _cachedExpectedVersionsOwnerPath = null;
        }

        public static ModuleInfo GetModule(string id)
        {
            foreach (var mod in GetActiveModules())
                if (mod.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                    return mod;

            return new ModuleInfo
            {
                Id = id,
                Name = "<unknown>",
                AppVersion = ApplicationVersion.Empty,
                Path = "<unknown>",
                IsOfficial = IsOfficialModuleId(id),
            };
        }

        public static bool IsLoaded(string id)
        {
            return GetModule(id).IsLoaded;
        }

        public static bool IsLoaded(params string[] ids)
        {
            if (ids == null || ids.Length == 0)
                return false;

            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (GetModule(id).IsLoaded)
                    return true;
            }

            return false;
        }

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
        //                 Expected dependency versions           //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Reads the expected version for a dependency from this mod's SubModule.xml.
        /// Returns "unknown" if not found or if parsing failed.
        /// </summary>
        public static string GetExpectedDependencyVersion(string dependencyModuleId)
        {
            EnsureExpectedDependencyVersionsLoaded();

            if (_cachedExpectedDependencyVersions == null)
                return UnknownVersionString;

            if (string.IsNullOrWhiteSpace(dependencyModuleId))
                return UnknownVersionString;

            if (_cachedExpectedDependencyVersions.TryGetValue(dependencyModuleId, out var v))
                return string.IsNullOrWhiteSpace(v) ? UnknownVersionString : v;

            return UnknownVersionString;
        }

        private static void EnsureExpectedDependencyVersionsLoaded()
        {
            try
            {
                var self = GetModule(SelfModuleId);
                var ownerPath = self.Path ?? string.Empty;

                if (
                    _cachedExpectedDependencyVersions != null
                    && string.Equals(
                        _cachedExpectedVersionsOwnerPath,
                        ownerPath,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                    return;

                _cachedExpectedVersionsOwnerPath = ownerPath;
                _cachedExpectedDependencyVersions = LoadExpectedDependencyVersions(ownerPath);

                Log.Info(
                    "[Deps] Expected versions loaded: "
                        + (
                            _cachedExpectedDependencyVersions == null
                                ? "null"
                                : _cachedExpectedDependencyVersions.Count.ToString()
                        )
                );
            }
            catch (Exception e)
            {
                _cachedExpectedDependencyVersions = null;
                Log.Exception(e, "Failed to load expected dependency versions from SubModule.xml.");
            }
        }

        private static Dictionary<string, string> LoadExpectedDependencyVersions(string moduleRoot)
        {
            if (string.IsNullOrWhiteSpace(moduleRoot) || moduleRoot == "<unknown>")
                return null;

            var subModulePath = Path.Combine(moduleRoot, "SubModule.xml");
            Log.Info("[Deps] Reading expected versions from: " + subModulePath);

            if (!File.Exists(subModulePath))
            {
                Log.Error("[Deps] SubModule.xml not found at: " + subModulePath);
                return null;
            }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in map)
                Log.Info("[Deps] Expected: " + kv.Key + " => " + kv.Value);

            XDocument doc;
            try
            {
                doc = XDocument.Load(subModulePath);
            }
            catch
            {
                return null;
            }

            var root = doc.Root;
            var deps = root?.Element("DependedModules");
            if (deps == null)
                return map;

            foreach (var e in deps.Elements("DependedModule"))
            {
                var id = e.Attribute("Id")?.Value;
                var ver = e.Attribute("DependentVersion")?.Value;

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                map[id] = ver ?? UnknownVersionString;
            }

            return map;
        }

        /// <summary>
        /// Returns true if actualVersion is >= expectedVersion (both strings like "v2.4.2" or "1.3.10").
        /// If parsing fails, returns false.
        /// </summary>
        public static bool IsVersionAtLeast(string actualVersion, string expectedVersion)
        {
            if (string.IsNullOrWhiteSpace(expectedVersion))
                return true;

            if (string.IsNullOrWhiteSpace(actualVersion))
                return false;

            if (actualVersion.Equals(UnknownVersionString, StringComparison.OrdinalIgnoreCase))
                return false;

            if (expectedVersion.Equals(UnknownVersionString, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!TryParseVersion(actualVersion, out var a))
                return false;

            if (!TryParseVersion(expectedVersion, out var b))
                return false;

            return CompareVersion(a, b) >= 0;
        }

        private readonly struct VersionParts(int major, int minor, int revision, int build)
        {
            public readonly int Major = major;
            public readonly int Minor = minor;
            public readonly int Revision = revision;
            public readonly int Build = build;
        }

        private static bool TryParseVersion(string s, out VersionParts v)
        {
            v = new VersionParts(0, 0, 0, 0);

            if (string.IsNullOrWhiteSpace(s))
                return false;

            var t = s.Trim();
            if (t.Length > 0 && (t[0] == 'v' || t[0] == 'V'))
                t = t.Substring(1);

            var parts = t.Split('.');
            if (parts.Length == 0)
                return false;

            int major = 0;
            int minor = 0;
            int revision = 0;
            int build = 0;

            if (parts.Length >= 1 && !int.TryParse(parts[0], out major))
                return false;

            if (parts.Length >= 2 && !int.TryParse(parts[1], out minor))
                return false;

            if (parts.Length >= 3 && !int.TryParse(parts[2], out revision))
                return false;

            if (parts.Length >= 4 && !int.TryParse(parts[3], out build))
                return false;

            v = new VersionParts(major, minor, revision, build);
            return true;
        }

        private static int CompareVersion(VersionParts a, VersionParts b)
        {
            if (a.Major != b.Major)
                return a.Major.CompareTo(b.Major);

            if (a.Minor != b.Minor)
                return a.Minor.CompareTo(b.Minor);

            if (a.Revision != b.Revision)
                return a.Revision.CompareTo(b.Revision);

            return a.Build.CompareTo(b.Build);
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
