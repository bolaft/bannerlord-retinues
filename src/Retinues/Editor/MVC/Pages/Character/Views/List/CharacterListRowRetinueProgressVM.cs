using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Behaviors.Retinues;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Character.Views.List
{
    /// <summary>
    /// Disabled informational row showing retinue unlock progress for a culture.
    /// Used as a pinned entry at the bottom of the Retinues header.
    /// </summary>
    public sealed class RetinueUnlockProgressRowVM(
        ListHeaderVM header,
        WCulture culture,
        int progress
    ) : BaseListRowVM(header, culture?.StringId ?? string.Empty)
    {
        private readonly WCulture _culture = culture;
        private readonly int _progress = Math.Max(0, progress);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Type Flags                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsRetinueUnlockProgress => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Enabled                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsEnabled => false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsSelected => false;

        /// <summary>
        /// No-op selection handler for a disabled informational row.
        /// </summary>
        [DataSourceMethod]
        public override void ExecuteSelect() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           UI                           //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name => _culture?.Name ?? string.Empty;

        [DataSourceProperty]
        public object BannerImage => _culture?.Image;

        /// <summary>
        /// Computes the culture unlock progress as a clamped percentage.
        /// </summary>
        [DataSourceProperty]
        public int ProgressPercent
        {
            get
            {
                var pct = (int)
                    Math.Round(
                        (_progress * 100.0) / RetinuesBehavior.UnlockProgressTarget,
                        MidpointRounding.AwayFromZero
                    );
                if (pct < 0)
                    return 0;
                if (pct > 100)
                    return 100;
                return pct;
            }
        }

        /// <summary>
        /// Gets a localized "Progress: X%" text for this row.
        /// </summary>
        [DataSourceProperty]
        public string ProgressText =>
            L.T("retinue_unlock_progress_row", "Progress: {PCT}%")
                .SetTextVariable("PCT", ProgressPercent)
                .ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Provides a pinned sort progress value to keep rows ordered by unlock progress.
        /// </summary>
        internal override bool TryGetPinnedSortProgress(out int progress)
        {
            progress = _progress;
            return progress > 0;
        }

        /// <summary>
        /// Returns the row sort value for the specified sort key.
        /// </summary>
        internal override IComparable GetSortValue(ListSortKey sortKey)
        {
            return sortKey switch
            {
                ListSortKey.Name => Name,
                _ => Name,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Filtering                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true when this row matches the provided filter text.
        /// </summary>
        internal override bool MatchesFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var f = filter.Trim();
            var comparison = StringComparison.OrdinalIgnoreCase;

            if (!string.IsNullOrEmpty(Name) && Name.IndexOf(f, comparison) >= 0)
                return true;

            var id = _culture?.StringId;
            if (!string.IsNullOrEmpty(id) && id.IndexOf(f, comparison) >= 0)
                return true;

            return false;
        }
    }
}
