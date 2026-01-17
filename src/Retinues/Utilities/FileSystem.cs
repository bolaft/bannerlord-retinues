using System;
using System.IO;
using System.Text;

namespace Retinues.Utilities
{
    /// <summary>
    /// File system helpers for common Retinues paths.
    /// </summary>
    public static class FileSystem
    {
        private const string RetinuesFolderName = "Retinues";

        private static readonly Lazy<string> _documentsDirectory = new(ResolveDocumentsDirectory);

        private static readonly Lazy<string> _retinuesDocumentsDirectory = new(
            ResolveRetinuesDocumentsDirectory
        );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Public                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// OS-appropriate "My Documents" directory (best-effort).
        /// </summary>
        public static string DocumentsDirectory => _documentsDirectory.Value;

        /// <summary>
        /// Ensured directory: "{My Documents}/Retinues".
        /// </summary>
        public static string RetinuesDocumentsDirectory => _retinuesDocumentsDirectory.Value;

        /// <summary>
        /// Gets a path under "{My Documents}/Retinues" and ensures the Retinues directory exists.
        /// If you pass additional segments, the parent directory of the returned path is ensured.
        /// </summary>
        public static string GetPathInRetinuesDocuments(params string[] relativeSegments)
        {
            var root = RetinuesDocumentsDirectory;

            if (relativeSegments == null || relativeSegments.Length == 0)
                return root;

            var path = root;
            for (int i = 0; i < relativeSegments.Length; i++)
            {
                var segment = relativeSegments[i];
                if (string.IsNullOrEmpty(segment))
                    continue;

                path = Path.Combine(path, segment);
            }

            // Ensure parent directory exists (works for both file and directory subpaths).
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent))
                EnsureDirectoryExists(parent);

            return path;
        }

        /// <summary>
        /// Sanitizes a string to be safe for use as a filename.
        /// Replaces invalid characters with underscores and falls back to the given string if the result is empty.
        /// </summary>
        public static string SanitizeFileName(string name, string fallback)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = fallback;

            // Replace invalid filename chars with '_'
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);

            for (int i = 0; i < name.Length; i++)
            {
                var ch = name[i];
                var bad = false;

                for (int j = 0; j < invalid.Length; j++)
                {
                    if (ch == invalid[j])
                    {
                        bad = true;
                        break;
                    }
                }

                sb.Append(bad ? '_' : ch);
            }

            var s = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(s) ? fallback : s;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Resolvers                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Resolves the OS-appropriate "My Documents" directory (best-effort).
        /// </summary>
        private static string ResolveDocumentsDirectory()
        {
            try
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (!string.IsNullOrEmpty(docs))
                    return docs;
            }
            catch
            {
                // fall through
            }

            // Some environments map "Documents" to "Personal".
            try
            {
                var personal = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                if (!string.IsNullOrEmpty(personal))
                    return personal;
            }
            catch
            {
                // fall through
            }

            // Best-effort fallback.
            try
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(home))
                    return Path.Combine(home, "Documents");
            }
            catch
            {
                // fall through
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Resolves the Retinues documents directory and ensures it exists.
        /// </summary>
        private static string ResolveRetinuesDocumentsDirectory()
        {
            var dir = Path.Combine(DocumentsDirectory, RetinuesFolderName);
            EnsureDirectoryExists(dir);
            return dir;
        }

        /// <summary>
        /// Ensures the given directory path exists.
        /// </summary>
        private static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch { }
        }
    }
}
