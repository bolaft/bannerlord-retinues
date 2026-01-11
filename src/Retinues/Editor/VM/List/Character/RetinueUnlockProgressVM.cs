using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Game.Retinues;
using Retinues.UI.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List.Character
{
    /// <summary>
    /// Disabled row shown for cultures whose retinue is being unlocked.
    /// </summary>
    public sealed class RetinueUnlockProgressRowVM : BaseListRowVM
    {
        private readonly WCulture _culture;
        private readonly int _progress;

        public RetinueUnlockProgressRowVM(ListHeaderVM header, WCulture culture, int progress)
            : base(header, culture?.StringId ?? string.Empty)
        {
            _culture = culture;
            _progress = Math.Max(0, progress);
        }

        [DataSourceProperty]
        public override bool IsRetinueUnlockProgress => true;

        [DataSourceProperty]
        public override bool IsEnabled => false;

        [DataSourceProperty]
        public override bool IsSelected => false;

        [DataSourceMethod]
        public override void ExecuteSelect() { }

        [DataSourceProperty]
        public string Name => _culture?.Name ?? string.Empty;

        [DataSourceProperty]
        public object BannerImage => _culture?.Image;

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

        [DataSourceProperty]
        public string ProgressText =>
            L.T("retinue_unlock_progress_row", "Progress: {PCT}%")
                .SetTextVariable("PCT", ProgressPercent)
                .ToString();

        internal int ProgressPoints => _progress;

        internal override IComparable GetSortValue(ListSortKey sortKey)
        {
            return sortKey switch
            {
                ListSortKey.Name => Name,
                _ => Name,
            };
        }

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
