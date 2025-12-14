using System;
using System.Text;
using Retinues.Configuration;
using TaleWorlds.Library;

namespace OldRetinues.Utils
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                    Bug Report (Email)                  //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Builds a prefilled bug report email using mailto: and opens it in the user's mail client.
    /// </summary>
    [SafeClass]
    public static class BugReportEmail
    {
        private const string SupportEmail = "retinues.support@proton.me";

        /// <summary>
        /// Opens the default mail client with a prefilled Retinues bug report draft.
        /// </summary>
        public static void OpenDraft()
        {
            try
            {
                // 1) Make sure config is in the log (so attaching debug.log is useful)
                try
                {
                    Config.LogDump();
                }
                catch (Exception e)
                {
                    Log.Warn($"BugReportEmail: Config.LogDump failed: {e}");
                }

                // 2) Collect environment info
                var gameVersion = BannerlordVersion.Version.ToString() ?? "unknown"; // 1.2.12, 1.3.7, etc.

                var retinuesModule = ModuleChecker.GetModule("Retinues"); // adjust ID if needed
                var retinuesVersion = retinuesModule?.Version ?? ModuleChecker.UnknownVersionString;

                var sb = new StringBuilder();

                sb.AppendLine(
                    "Please describe what happened **above** this line if your mail client allows it."
                );
                sb.AppendLine();
                sb.AppendLine("-------------------------------------------------------------");
                sb.AppendLine("Environment");
                sb.AppendLine($"- Game version: {gameVersion}");
                sb.AppendLine($"- Retinues version: {retinuesVersion}");
                sb.AppendLine();

                sb.AppendLine("Active modules (load order):");
                foreach (var mod in ModuleChecker.GetActiveModules())
                {
                    var officialTag = mod.IsOfficial ? " (official)" : "";
                    sb.AppendLine($"- {mod.Id} [{mod.Version}] - {mod.Name}{officialTag}");
                }

                sb.AppendLine();
                sb.AppendLine("Debug log");
                sb.AppendLine("- Please attach your Retinues debug.log file to this email.");
                sb.AppendLine(
                    "- If you are unsure where it is, check the Retinues readme / troubleshooting section."
                );
                sb.AppendLine();
                sb.AppendLine("Additional info");
                sb.AppendLine("- What were you doing when the bug happened?");
                sb.AppendLine("- Can you reproduce it? If yes, list the steps.");
                sb.AppendLine("- Any other mods that might be related?");

                // 3) Build mailto URL with URL-encoded subject/body
                var subject = Uri.EscapeDataString(
                    $"Retinues bug report (BL {gameVersion}, Retinues {retinuesVersion})"
                );

                var body = Uri.EscapeDataString(sb.ToString());

                var url = $"mailto:{SupportEmail}?subject={subject}&body={body}";

                // 4) Open via existing URL helper
                URL.OpenInBrowser(url);
            }
            catch (Exception e)
            {
                Log.Exception(e);
                InformationManager.DisplayMessage(
                    new InformationMessage("Failed to open bug report email. See log for details.")
                );
            }
        }
    }
}
