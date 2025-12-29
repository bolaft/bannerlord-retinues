using System;
using System.IO;
using TaleWorlds.Library;

namespace Retinues.Modules.Submods
{
    /// <summary>
    /// Finds install locations relevant to writing a Bannerlord module.
    /// This is the generic extraction of the in-controller logic.
    /// </summary>
    public static class SubmodEnvironment
    {
        public static bool TryGetGameModulesDirectory(out string modulesDir)
        {
            modulesDir = string.Empty;

            try
            {
                // Best option in-game: resolves actual install base (Steam, GOG, custom library, etc.)
                var basePath = BasePath.Name;
                if (!string.IsNullOrWhiteSpace(basePath))
                {
                    var candidate = Path.GetFullPath(Path.Combine(basePath, "Modules"));
                    if (Directory.Exists(candidate))
                    {
                        modulesDir = candidate;
                        return true;
                    }
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                // Fallback: walk up from bin folder until we find "Modules"
                var dir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 8 && !string.IsNullOrWhiteSpace(dir); i++)
                {
                    var candidate = Path.Combine(dir, "Modules");
                    if (Directory.Exists(candidate))
                    {
                        modulesDir = Path.GetFullPath(candidate);
                        return true;
                    }

                    dir = Directory.GetParent(dir)?.FullName;
                }
            }
            catch
            {
                // fall through
            }

            return false;
        }

        /// <summary>
        /// Conservative, launcher-friendly module id sanitizer.
        /// Mirrors the controller logic but moved to a reusable location.
        /// </summary>
        public static string SanitizeModuleId(string raw, string fallback = "Submod")
        {
            raw ??= string.Empty;

            var s = raw.Trim();

            // Convert whitespace to underscores.
            s = string.Join(
                "_",
                s.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            );

            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c.ToString(), string.Empty);

            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                var ch = chars[i];
                var ok =
                    (ch >= 'a' && ch <= 'z')
                    || (ch >= 'A' && ch <= 'Z')
                    || (ch >= '0' && ch <= '9')
                    || ch == '_'
                    || ch == '-';

                if (!ok)
                    chars[i] = '_';
            }

            s = new string(chars).Trim('_');

            if (string.IsNullOrWhiteSpace(s))
                s = string.IsNullOrWhiteSpace(fallback) ? "Submod" : fallback;

            return s;
        }
    }
}
