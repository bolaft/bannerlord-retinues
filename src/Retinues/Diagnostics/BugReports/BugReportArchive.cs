using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Retinues.Diagnostics.BugReports
{
    internal sealed class BugReportFile
    {
        internal string Source;
        internal string Entry;
        internal bool IsLog;
    }

    internal sealed class BugReportResult
    {
        internal string Path;
        internal bool HasSave;
        internal bool HasScreenshot;
        internal readonly List<string> Notes = new();
    }

    /// <summary>Filesystem-only packaging. Never saves or changes campaign data.</summary>
    internal static class BugReportArchive
    {
        private const long MaxFileBytes = 512L * 1024 * 1024;
        private const long MaxTotalBytes = 2L * 1024 * 1024 * 1024;

        // Use only the active slot; never guess another campaign from the newest .sav file.
        internal static string ResolveSavePath(string directory, string slot)
        {
            if (string.IsNullOrWhiteSpace(slot) || slot.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || slot == "." || slot == ".." || slot.Contains("/") || slot.Contains("\\"))
                return null;
            return Path.Combine(directory, slot + ".sav");
        }

        internal static bool IsSafeEntry(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains("\\") || name.Contains(":"))
                return false;
            foreach (var part in name.Split('/'))
                if (string.IsNullOrEmpty(part) || part == "." || part == "..")
                    return false;
            return true;
        }

        internal static BugReportResult Create(string outputDirectory, string report,
            IEnumerable<BugReportFile> files, IEnumerable<string> notes)
        {
            Directory.CreateDirectory(outputDirectory);
            var id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var destination = Path.Combine(outputDirectory, "Retinues-BugReport-" + id + ".zip");
            var partial = destination + ".partial";
            var staging = Path.Combine(outputDirectory, ".staging-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            var result = new BugReportResult { Path = destination };
            result.Notes.AddRange(notes);
            var included = new List<string>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "report.txt" };
            long total = 0;
            try
            {
                using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var archive = new ZipArchive(output, ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                    {
                        if (!IsSafeEntry(file.Entry) || !names.Add(file.Entry))
                        {
                            result.Notes.Add("Skipped an invalid or duplicate archive entry.");
                            continue;
                        }
                        var snapshot = Path.Combine(staging, "snapshot");
                        long length;
                        DateTime modified;
                        try
                        {
                            // Sharing permits the game to continue writing. Copy first, then validate
                            // saves/settings; never hold a source save open during ZIP compression.
                            modified = File.GetLastWriteTimeUtc(file.Source);
                            using (var source = new FileStream(file.Source, FileMode.Open, FileAccess.Read,
                                FileShare.ReadWrite | FileShare.Delete))
                            using (var target = new FileStream(snapshot, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                length = source.Length;
                                if (length > MaxFileBytes || length > MaxTotalBytes - total)
                                    throw new IOException("File exceeds the report size limit.");
                                CopySnapshot(source, target, length);
                                if (!file.IsLog && (source.Length != length
                                    || File.GetLastWriteTimeUtc(file.Source) != modified))
                                    throw new IOException("File changed while being collected; try again after saving finishes.");
                            }
                        }
                        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                        {
                            result.Notes.Add("Not included: " + file.Entry + " (" + e.GetType().Name + "): " + e.Message);
                            continue;
                        }

                        var entry = archive.CreateEntry(file.Entry, CompressionLevel.Optimal);
                        using (var source = File.OpenRead(snapshot))
                        using (var target = entry.Open())
                            source.CopyTo(target);
                        total += length;
                        included.Add(file.Entry + " | " + length + " bytes | modified " + modified.ToString("u"));
                        result.HasSave |= file.Entry.StartsWith("save/", StringComparison.Ordinal);
                        result.HasScreenshot |= file.Entry.StartsWith("screenshot/", StringComparison.Ordinal);
                    }

                    var text = new StringBuilder(report);
                    text.AppendLine().AppendLine("Included files:");
                    foreach (var line in included) text.AppendLine(line);
                    text.AppendLine().AppendLine("Collection notes:");
                    if (result.Notes.Count == 0) text.AppendLine("All requested files were collected.");
                    foreach (var note in result.Notes) text.AppendLine(note);
                    using (var writer = new StreamWriter(archive.CreateEntry("report.txt").Open(), new UTF8Encoding(false)))
                        writer.Write(text.ToString());
                }
                // A final .zip is visible only after the central directory has been written.
                File.Move(partial, destination);
                return result;
            }
            finally
            {
                // Both paths are uniquely created by this operation, never user input.
                try { if (File.Exists(partial)) File.Delete(partial); } catch { }
                try { Directory.Delete(staging, true); } catch { }
            }
        }

        internal static void CopySnapshot(Stream source, Stream target, long length)
        {
            var buffer = new byte[81920];
            while (length > 0)
            {
                int read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, length));
                if (read == 0) throw new EndOfStreamException("File shrank while being collected.");
                target.Write(buffer, 0, read);
                length -= read;
            }
        }
    }
}
