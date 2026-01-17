using System;
using System.Diagnostics;
using System.IO;

namespace Retinues.Utilities
{
    /// <summary>
    /// Shell utilities for opening files.
    /// </summary>
    public static class Shell
    {
        /// <summary>
        /// Opens the specified file for editing using the system's default editor.
        /// </summary>
        public static void OpenForEdit(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("File not found.", path);

            // 1) Try the shell "edit" verb (Notepad++ etc).
            var psi = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "edit",
            };

            try
            {
                Process.Start(psi);
                return;
            }
            catch
            {
                // 2) Fall back to the Windows "Open with..." dialog.
                // This is more reliable than the "edit" verb on some machines.
            }

            var openAs = new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = "shell32.dll,OpenAs_RunDLL " + Quote(path),
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            Process.Start(openAs);
        }

        /// <summary>
        /// Quotes a string for use in command-line arguments.
        /// </summary>
        private static string Quote(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "\"\"";
            if (
                s.StartsWith("\"", StringComparison.Ordinal)
                && s.EndsWith("\"", StringComparison.Ordinal)
            )
                return s;
            return "\"" + s.Replace("\"", "\\\"") + "\"";
        }
    }
}
