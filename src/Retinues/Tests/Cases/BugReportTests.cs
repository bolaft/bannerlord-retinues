using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Retinues.Diagnostics.BugReports;

namespace Retinues.Tests.Cases
{
    public static class BugReportTests
    {
        private static void InTemp(Action<string> test)
        {
            var parent = Environment.GetEnvironmentVariable("RETINUES_TEST_DIRECTORY") ?? Path.GetTempPath();
            var root = Path.Combine(parent, "Retinues-BugReportTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try { test(root); }
            finally { Directory.Delete(root, true); }
        }

        private static string Read(ZipArchive zip, string name)
        {
            using (var reader = new StreamReader(zip.GetEntry(name).Open())) return reader.ReadToEnd();
        }

        [GameTest("ReportPreservesSourcesAndDocumentsContents", "bug-reports", RequiresCampaign = false)]
        public static void ReportPreservesSourcesAndDocumentsContents()
        {
            InTemp(root =>
            {
                var source = Path.Combine(root, "campaign.sav");
                var bytes = Enumerable.Range(0, 35000).Select(i => (byte)(i % 251)).ToArray();
                File.WriteAllBytes(source, bytes);
                var modified = File.GetLastWriteTimeUtc(source);
                var log = Path.Combine(root, "debug.log");
                File.WriteAllText(log, "Old error\nNew session: no errors\n");
                var settings = Path.Combine(root, "settings.ini");
                File.WriteAllText(settings, "setting=true\n");
                var screenshot = Path.Combine(root, "screen.png");
                File.WriteAllBytes(screenshot, new byte[] { 137, 80, 78, 71 });
                var files = new[]
                {
                    new BugReportFile { Source = source, Entry = "save/campaign.sav" },
                    new BugReportFile { Source = log, Entry = "logs/Retinues/debug.log", IsLog = true },
                    new BugReportFile { Source = settings, Entry = "settings.ini" },
                    new BugReportFile { Source = screenshot, Entry = "screenshot/game.png" },
                };
                var output = Path.Combine(root, "reports");
                var result = BugReportArchive.Create(output, "Problem: levées disappear\nMod order: A, B\n", files, Array.Empty<string>());
                Tests.AssertTrue(result.HasSave && result.HasScreenshot);
                Tests.AssertEqual(0, result.Notes.Count);
                using (var stream = File.OpenRead(result.Path))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    Tests.AssertEqual(5, zip.Entries.Count);
                    using (var copy = new MemoryStream())
                    {
                        using (var entry = zip.GetEntry("save/campaign.sav").Open()) entry.CopyTo(copy);
                        Tests.AssertTrue(bytes.SequenceEqual(copy.ToArray()), "Save copy is byte-identical.");
                    }
                    Tests.AssertEqual(File.ReadAllText(log), Read(zip, "logs/Retinues/debug.log"));
                    Tests.AssertTrue(Read(zip, "report.txt").Contains("levées disappear"));
                    Tests.AssertTrue(Read(zip, "report.txt").Contains("save/campaign.sav | 35000 bytes"));
                }
                Tests.AssertTrue(bytes.SequenceEqual(File.ReadAllBytes(source)), "Original save was not rewritten.");
                Tests.AssertEqual(modified, File.GetLastWriteTimeUtc(source));
                Tests.AssertEqual(0, Directory.GetDirectories(output).Length, "Staging was cleaned.");
                Tests.AssertEqual(0, Directory.GetFiles(output, "*.partial").Length);
                var second = BugReportArchive.Create(output, "Second report", files, Array.Empty<string>());
                Tests.AssertFalse(result.Path == second.Path, "Repeat reports never overwrite one another.");
                Tests.AssertTrue(File.Exists(result.Path));
            });
        }

        [GameTest("MissingLockedAndOversizeFilesProduceUsablePartialReport", "bug-reports", RequiresCampaign = false)]
        public static void MissingLockedAndOversizeFilesProduceUsablePartialReport()
        {
            InTemp(root =>
            {
                var locked = Path.Combine(root, "locked.sav");
                var oversized = Path.Combine(root, "oversized.log");
                using (var file = File.Create(oversized)) file.SetLength(512L * 1024 * 1024 + 1);
                using (var held = new FileStream(locked, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    var result = BugReportArchive.Create(Path.Combine(root, "reports"), "Crash details", new[]
                    {
                        new BugReportFile { Source = locked, Entry = "save/locked.sav" },
                        new BugReportFile { Source = Path.Combine(root, "missing.png"), Entry = "screenshot/game.png" },
                        new BugReportFile { Source = oversized, Entry = "logs/oversized.log", IsLog = true },
                    }, new[] { "Campaign has unsaved progress." });
                    Tests.AssertFalse(result.HasSave || result.HasScreenshot);
                    Tests.AssertEqual(4, result.Notes.Count);
                    using (var stream = File.OpenRead(result.Path))
                    using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        Tests.AssertEqual(1, zip.Entries.Count, "Failed sources do not leave corrupt or partial ZIP entries.");
                        var text = Read(zip, "report.txt");
                        Tests.AssertTrue(text.Contains("Not included: save/locked.sav"));
                        Tests.AssertTrue(text.Contains("Not included: screenshot/game.png"));
                        Tests.AssertTrue(text.Contains("Campaign has unsaved progress."));
                    }
                }
            });
        }

        [GameTest("ArchiveRejectsUnsafeAndDuplicateEntryNames", "bug-reports", RequiresCampaign = false)]
        public static void ArchiveRejectsUnsafeAndDuplicateEntryNames()
        {
            InTemp(root =>
            {
                var source = Path.Combine(root, "source.txt");
                File.WriteAllText(source, "evidence");
                var files = new List<BugReportFile>();
                foreach (var name in new[] { "../outside.txt", "/root.txt", "C:/root.txt", "logs/../outside.txt", "logs\\file.txt", "report.txt", "logs/file.txt", "LOGS/FILE.TXT" })
                    files.Add(new BugReportFile { Source = source, Entry = name });
                var result = BugReportArchive.Create(Path.Combine(root, "reports"), "Report", files, Array.Empty<string>());
                using (var stream = File.OpenRead(result.Path))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    Tests.AssertEqual(2, zip.Entries.Count);
                    Tests.AssertEqual("evidence", Read(zip, "logs/file.txt"));
                    Tests.AssertTrue(zip.Entries.All(e => BugReportArchive.IsSafeEntry(e.FullName)));
                }
                Tests.AssertEqual(7, result.Notes.Count);
            });
        }

        [GameTest("SaveSelectionNeverFallsBackToAnotherCampaign", "bug-reports", RequiresCampaign = false)]
        public static void SaveSelectionNeverFallsBackToAnotherCampaign()
        {
            foreach (var slot in new[] { null, "", "..", "../other", "folder/other", "folder\\other", "C:\\other" })
                Tests.AssertTrue(BugReportArchive.ResolveSavePath(Path.GetTempPath(), slot) == null);
            Tests.AssertEqual(Path.Combine(Path.GetTempPath(), "Été 1084.sav"),
                BugReportArchive.ResolveSavePath(Path.GetTempPath(), "Été 1084"));
        }

        [GameTest("GrowingLogsAreBoundedAndTruncatedFilesAreRejected", "bug-reports", RequiresCampaign = false)]
        public static void GrowingLogsAreBoundedAndTruncatedFilesAreRejected()
        {
            using (var source = new MemoryStream(Encoding.UTF8.GetBytes("beforeAFTER")))
            using (var target = new MemoryStream())
            {
                BugReportArchive.CopySnapshot(source, target, 6);
                Tests.AssertEqual("before", Encoding.UTF8.GetString(target.ToArray()));
            }
            using (var source = new MemoryStream(new byte[] { 1, 2 }))
            using (var target = new MemoryStream())
                Tests.AssertThrows<EndOfStreamException>(() => BugReportArchive.CopySnapshot(source, target, 3));
        }
    }
}
