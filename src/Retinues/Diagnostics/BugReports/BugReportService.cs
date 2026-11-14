using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Retinues.Framework.Modules;
using Retinues.Interface.Services;
using Retinues.Interface.Services.Popups;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using FileSystem = Retinues.Utilities.FileSystem;

namespace Retinues.Diagnostics.BugReports
{
    /// <summary>Game APIs run on the application thread; only file collection runs in the worker.</summary>
    public static class BugReportService
    {
        private sealed class Request
        {
            internal string Description;
            internal readonly Stopwatch Delay = Stopwatch.StartNew();
            internal int DelayMilliseconds;
            internal Campaign Campaign;
        }

        private static Request _request;
        private static Task<BugReportResult> _worker;

        internal static void ShowDialog()
        {
            if (_request != null || _worker != null)
            {
                Inquiries.Popup(L.T("bug_report_title", "Bug Report"),
                    L.T("bug_report_busy", "A bug report is already being created."));
                return;
            }
            try
            {
                BugReportPopupLayer.Show(description => Queue(description, 600)?.ToString());
            }
            catch (Exception e)
            {
                Log.Exception(e, "Could not open the bug report dialog.");
                Inquiries.Popup(L.T("bug_report_title", "Bug Report"),
                    L.T("bug_report_failed", "Could not create the bug report. Check the Retinues log and available disk space."));
            }
        }

        private static TaleWorlds.Localization.TextObject Queue(string description, int delayMilliseconds)
        {
            if (_request != null || _worker != null)
                return L.T("bug_report_busy", "A bug report is already being created.");
            if (Campaign.Current?.SaveHandler?.IsSaving == true)
                return L.T("bug_report_saving", "Please wait for the current save to finish, then create the report again.");
            _request = new Request
            {
                Description = description ?? "",
                DelayMilliseconds = delayMilliseconds,
                Campaign = Campaign.Current,
            };
            return null;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("bug_report", "retinues")]
        public static string CreateReport(List<string> args)
        {
            try
            {
                var failure = Queue(args == null ? "" : string.Join(" ", args), 3000);
                return failure?.ToString() ?? L.S("bug_report_console_queued",
                    "Report queued. Close the console: the game screenshot will be taken in 3 seconds. Logs, settings, the mod list and the active campaign's last saved file will be copied into a local ZIP; nothing is uploaded.");
            }
            catch (Exception e)
            {
                Log.Exception(e, "Could not queue bug report.");
                return L.S("bug_report_failed", "Could not create the bug report. Check the Retinues log and available disk space.");
            }
        }

        internal static void Tick()
        {
            if (_request == null && _worker == null) return;
            try
            {
                if (_request != null && _request.Delay.ElapsedMilliseconds >= _request.DelayMilliseconds)
                {
                    var request = _request;
                    _request = null;
                    // A load or exit during the screenshot delay must not bundle a different campaign.
                    if (!ReferenceEquals(request.Campaign, Campaign.Current))
                        throw new InvalidOperationException("The campaign changed before collection started.");
                    Start(request);
                }
                if (_worker?.IsCompleted == true)
                {
                    var task = _worker;
                    _worker = null;
                    var result = task.GetAwaiter().GetResult();
                    Log.Info("Bug report created: " + result.Path);
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = Path.GetDirectoryName(result.Path), UseShellExecute = true,
                        });
                    }
                    catch (Exception e) { Log.Exception(e, "Could not open bug report folder."); }
                    var body = L.T("bug_report_complete",
                        "Your bug report ZIP is ready:\n{PATH}\n\nReview it, then submit a report in the Bugs tab on the Retinues Nexus Mods page, including this ZIP or a download link. Nothing has been uploaded. Collection details are in report.txt.")
                        .SetTextVariable("PATH", result.Path).ToString();
                    if (!result.HasSave) body += "\n\n" + L.S("bug_report_no_save", "No save could be included. Attach the relevant save yourself if needed.");
                    if (!result.HasScreenshot) body += "\n\n" + L.S("bug_report_no_screenshot", "No screenshot could be captured. Attach one yourself if it helps explain the problem.");
                    if (result.Notes.Count > 0) body += "\n\n" + L.S("bug_report_partial", "Some files were unavailable; see report.txt for details.");
                    Inquiries.Popup(L.T("bug_report_title", "Bug Report"), new TaleWorlds.Localization.TextObject(body));
                }
            }
            catch (Exception e)
            {
                _request = null;
                Log.Exception(e, "Bug report collection failed.");
                Inquiries.Popup(L.T("bug_report_title", "Bug Report"),
                    L.T("bug_report_failed", "Could not create the bug report. Check the Retinues log and available disk space."));
            }
        }

        private static void Start(Request request)
        {
            var directory = FileSystem.GetPathInRetinuesDocuments("BugReports");
            Directory.CreateDirectory(directory);
            var files = new List<BugReportFile>();
            var notes = new List<string>();
            var report = new StringBuilder("Retinues bug report\nCreated (UTC): " + DateTime.UtcNow.ToString("u"));
            report.AppendLine().AppendLine().AppendLine("Problem / steps to reproduce:")
                .AppendLine(string.IsNullOrWhiteSpace(request.Description) ? "Not provided." : request.Description);
            report.AppendLine().AppendLine("Save files are copies of existing saves; unsaved progress is not included.")
                .AppendLine("Logs may contain older sessions and local file paths. This report was not uploaded automatically.")
                .AppendLine("Submit your report in the Bugs tab on the Retinues Nexus Mods page, including this ZIP or a download link:")
                .AppendLine("https://www.nexusmods.com/mountandblade2bannerlord/mods/8847?tab=bugs");
            Collect("Game version", notes, () => report.AppendLine("Bannerlord: " + ApplicationVersion.FromParametersFile()));
            Collect("Mod list", notes, () =>
            {
                report.AppendLine().AppendLine("Active modules in load order:");
                foreach (var mod in ModuleManager.GetActiveModules()) report.AppendLine(mod.ToString());
            });
            Collect("Save", notes, () =>
            {
                if (Campaign.Current == null) { notes.Add("No campaign is loaded; no save included."); return; }
                if (Campaign.Current.SaveHandler?.IsSaving == true) { notes.Add("Save in progress; no save included. Retry after saving finishes."); return; }
                // The property is public in BL13+, private in BL12.
                var slot = typeof(MBSaveLoad).GetProperty("ActiveSaveSlotName",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) as string;
                if (slot == null)
                    slot = typeof(MBSaveLoad).GetField("ActiveSaveSlotName",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) as string;
                var path = BugReportArchive.ResolveSavePath(Path.Combine(FileSystem.DocumentsDirectory,
                    "Mount and Blade II Bannerlord", "Game Saves"), slot);
                if (path == null) { notes.Add("No active save slot; save this campaign first to include a save."); return; }
                report.AppendLine("Active save slot: " + slot);
                files.Add(new BugReportFile { Source = path, Entry = "save/" + Path.GetFileName(path) });
            });
            Collect("Retinues logs", notes, () =>
            {
                files.Add(new BugReportFile { Source = Log.FilePath, Entry = "logs/Retinues/debug.log", IsLog = true });
                var logDir = Path.GetDirectoryName(Path.GetFullPath(Log.FilePath));
                foreach (var path in Directory.EnumerateFiles(logDir, "*.log"))
                    if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(Log.FilePath), StringComparison.OrdinalIgnoreCase))
                        files.Add(new BugReportFile { Source = path, Entry = "logs/Retinues/" + Path.GetFileName(path), IsLog = true });
            });
            Collect("Settings", notes, () => files.Add(new BugReportFile
                { Source = ConfigurationPersistence.ConfigPath, Entry = "settings.ini" }));
            Collect("Game logs", notes, () =>
            {
                var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Mount and Blade II Bannerlord", "logs");
                if (!Directory.Exists(root)) { notes.Add("Bannerlord log folder was not found."); return; }
                // Current and previous session per log kind; never recurse into unrelated directories or crash dumps.
                var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prefix in new[] { "rgl_log_", "rgl_log_errors_", "watchdog_log_" })
                    foreach (var path in Directory.EnumerateFiles(root, prefix + "*.txt")
                        .Where(p => long.TryParse(Path.GetFileNameWithoutExtension(p).Substring(prefix.Length), out _))
                        .OrderByDescending(File.GetLastWriteTimeUtc).Take(2))
                        if (selected.Add(path)) files.Add(new BugReportFile
                            { Source = path, Entry = "logs/Bannerlord/" + Path.GetFileName(path), IsLog = true });
            });

            // Ask only the game renderer for an image, never capture the desktop or other windows.
            string screenshot = null;
            Collect("Screenshot", notes, () =>
            {
                var png = typeof(TaleWorlds.Engine.Utilities).GetMethod("TakeScreenshotAsPng", new[] { typeof(string) });
                screenshot = Path.Combine(directory, ".capture-" + Guid.NewGuid().ToString("N") + (png == null ? ".bmp" : ".png"));
                if (png != null) png.Invoke(null, new object[] { screenshot });
                else TaleWorlds.Engine.Utilities.TakeScreenshot(screenshot);
            });
            var reportText = report.ToString();
            _worker = Task.Run(() =>
            {
                try
                {
                    if (screenshot != null && WaitForScreenshot(screenshot))
                        files.Add(new BugReportFile { Source = screenshot, Entry = "screenshot/game" + Path.GetExtension(screenshot) });
                    else notes.Add("Game screenshot unavailable; attach a screenshot manually if useful.");
                    return BugReportArchive.Create(directory, reportText, files, notes);
                }
                finally
                {
                    try { if (screenshot != null && File.Exists(screenshot)) File.Delete(screenshot); } catch { }
                }
            });
        }

        private static void Collect(string label, List<string> notes, Action collect)
        {
            try { collect(); }
            catch (Exception e) { notes.Add(label + " unavailable (" + e.GetType().Name + ")."); }
        }

        private static bool WaitForScreenshot(string path)
        {
            var watch = Stopwatch.StartNew();
            long previous = -1;
            while (watch.ElapsedMilliseconds < 5000)
            {
                try
                {
                    using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (file.Length > 0 && file.Length == previous) return true;
                        previous = file.Length;
                    }
                }
                catch (IOException) { previous = -1; }
                catch (UnauthorizedAccessException) { return false; }
                Thread.Sleep(100);
            }
            return false;
        }
    }
}
