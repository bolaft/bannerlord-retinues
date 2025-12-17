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
        public override void RegisterEvents()
        {
            Log.Info("[Deps] RegisterEvents()");
            Hook(BehaviorEvent.GameLoadFinished, OnGameLoadFinished);
        }

        private void OnGameLoadFinished()
        {
            Log.Info("[Deps] OnGameLoadFinished() -> CheckDependencies(showPopup=true)");
            CheckDependencies(showPopup: true);
        }

        /// <summary>
        /// Checks all dependencies, logs issues, and optionally shows a popup.
        /// </summary>
        public static void CheckDependencies(bool showPopup)
        {
            Log.Info("[Deps] CheckDependencies(showPopup=" + showPopup + ")");
            var missing = new List<Dependency>();
            var versionMismatch = new List<Dependency>();
            var notInitialized = new List<Dependency>();

            foreach (var dep in SubModule.Dependencies)
            {
                Log.Info(
                    "[Deps] "
                        + dep.DisplayName
                        + " id="
                        + dep.ModuleId
                        + " kind="
                        + dep.Kind
                        + " loaded="
                        + dep.IsModuleLoaded
                        + " expected="
                        + dep.ExpectedVersion
                        + " actual="
                        + dep.ActualVersion
                        + " satisfied="
                        + dep.IsVersionSatisfied
                        + " initialized="
                        + dep.IsInitialized
                        + " state="
                        + dep.State
                );
                switch (dep.State)
                {
                    case DependencyState.MissingModule:
                        if (dep.Kind != DependencyKind.Optional)
                            missing.Add(dep);
                        break;

                    case DependencyState.VersionMismatch:
                        if (dep.Kind != DependencyKind.Optional)
                            versionMismatch.Add(dep);
                        break;

                    case DependencyState.PresentButNotInitialized:
                        notInitialized.Add(dep);
                        break;

                    case DependencyState.Initialized:
                    default:
                        break;
                }
            }

            var summary = BuildStatusSummary(missing, versionMismatch, notInitialized);

            if (missing.Count == 0 && versionMismatch.Count == 0 && notInitialized.Count == 0)
            {
                Log.Info(summary.ToString());
                return;
            }

            var errorText = L.T(
                    "dependencies_issues_detected",
                    "Dependency issues detected: {DETAILS}"
                )
                .SetTextVariable("DETAILS", summary);

            Log.Error(errorText.ToString());

            if (!showPopup)
                return;

            ShowWarningPopup(summary);
        }

        private static TextObject BuildStatusSummary(
            List<Dependency> missing,
            List<Dependency> versionMismatch,
            List<Dependency> notInitialized
        )
        {
            if (
                (missing == null || missing.Count == 0)
                && (versionMismatch == null || versionMismatch.Count == 0)
                && (notInitialized == null || notInitialized.Count == 0)
            )
            {
                return L.T("dependencies_all_ok", "All dependencies initialized correctly.");
            }

            var parts = new List<string>();

            if (missing != null && missing.Count > 0)
            {
                var label = L.S("dependencies_missing_label", "Missing");
                parts.Add(label + ": " + string.Join(", ", missing.Select(d => d.DisplayName)));
            }

            if (versionMismatch != null && versionMismatch.Count > 0)
            {
                var label = L.S("dependencies_version_mismatch_label", "Wrong version");

                var items = versionMismatch
                    .Select(d => string.Format("{0} {1}", d.DisplayName, d.GetVersionDiagnostic()))
                    .ToList();

                parts.Add(label + ": " + string.Join(", ", items));
            }

            if (notInitialized != null && notInitialized.Count > 0)
            {
                var label = L.S("dependencies_not_initialized_label", "Not initialized");
                parts.Add(
                    label + ": " + string.Join(", ", notInitialized.Select(d => d.DisplayName))
                );
            }

            var summary = string.Join(". ", parts);
            if (summary.Length > 0 && !summary.EndsWith("."))
                summary += ".";

            return new TextObject(summary);
        }

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
