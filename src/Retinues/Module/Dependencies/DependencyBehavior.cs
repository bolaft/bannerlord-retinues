using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors;
using Retinues.Helpers;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.Module.Dependencies
{
    /// <summary>
    /// Campaign behavior that inspects Retinues' external dependencies and checks their status.
    /// - Logs issues as soon as a campaign/session is launched.
    /// - Shows a descriptive warning popup when a game is started/loaded.
    /// </summary>
    public class DependencyBehavior : BaseCampaignBehavior
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Hooks                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Register events:
        /// - SessionLaunched: log dependency status as soon as a campaign session is ready.
        /// - GameLoadFinished: log again and show a popup if there are issues.
        /// </summary>
        public override void RegisterEvents()
        {
            Hook(BehaviorEvent.GameLoadFinished, OnGameLoadFinished);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnGameLoadFinished()
        {
            CheckDependencies(showPopup: true);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Core check logic                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Checks all dependencies, logs issues, and optionally shows a popup.
        /// </summary>
        public static void CheckDependencies(bool showPopup)
        {
            var missing = new List<Dependency>();
            var notInitialized = new List<Dependency>();

            foreach (var dep in SubModule.Dependencies)
            {
                switch (dep.State)
                {
                    case DependencyState.MissingModule:
                        if (dep.Kind != DependencyKind.Optional)
                            missing.Add(dep);
                        break;

                    case DependencyState.PresentButNotInitialized:
                        notInitialized.Add(dep);
                        break;

                    case DependencyState.Initialized:
                    default:
                        break;
                }
            }

            var summary = BuildStatusSummary(missing, notInitialized);

            if (missing.Count == 0 && notInitialized.Count == 0)
            {
                Log.Debug("[DependencyBehavior] " + summary.ToString());
                return;
            }

            var errorText = L.T(
                    "dependencies_issues_detected",
                    "Dependency issues detected: {DETAILS}"
                )
                .SetTextVariable("DETAILS", summary);

            Log.Error("[DependencyBehavior] " + errorText.ToString());

            if (!showPopup)
                return;

            ShowWarningPopup(summary);
        }

        /// <summary>
        /// Builds a summary TextObject describing the current dependency issues.
        /// </summary>
        private static TextObject BuildStatusSummary(
            List<Dependency> missing,
            List<Dependency> notInitialized
        )
        {
            if (
                (missing == null || missing.Count == 0)
                && (notInitialized == null || notInitialized.Count == 0)
            )
            {
                return L.T("dependencies_all_ok", "All dependencies initialized correctly.");
            }

            var parts = new List<string>();

            if (missing != null && missing.Count > 0)
            {
                var missingLabel = L.S("dependencies_missing_label", "Missing");

                parts.Add(
                    missingLabel + ": " + string.Join(", ", missing.Select(d => d.DisplayName))
                );
            }

            if (notInitialized != null && notInitialized.Count > 0)
            {
                var notInitLabel = L.S("dependencies_not_initialized_label", "Not initialized");

                parts.Add(
                    notInitLabel
                        + ": "
                        + string.Join(", ", notInitialized.Select(d => d.DisplayName))
                );
            }

            var summary = string.Join(". ", parts);
            if (summary.Length > 0 && !summary.EndsWith("."))
                summary += ".";

            // Dynamic sentence composed from localized labels + module display names.
            return new TextObject(summary);
        }

        /// <summary>
        /// Shows a warning popup with the given summary of issues.
        /// </summary>
        private static void ShowWarningPopup(TextObject summary)
        {
            try
            {
                var title = L.T("init_error_title", "Retinues Dependency Error");

                var body = L.T(
                        "init_error_body_detailed",
                        "Retinues mod dependencies failed to initialize properly:\n\n{DETAILS}\n\nThe mod may not function correctly until the dependencies are fixed."
                    )
                    .SetTextVariable("DETAILS", summary);

                Inquiries.Popup(title, body);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Error while showing dependency warning popup.");
            }
        }
    }
}
