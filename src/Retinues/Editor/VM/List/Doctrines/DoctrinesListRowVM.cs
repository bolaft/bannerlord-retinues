using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Editor.Events;
using Retinues.Game.Doctrines;
using Retinues.UI.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List.Doctrines
{
    /// <summary>
    /// Row representing a doctrine in the list.
    /// </summary>
    public sealed class DoctrinesListRowVM(ListHeaderVM header, string doctrineId)
        : BaseListRowVM(header, doctrineId ?? string.Empty)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly string _doctrineId = doctrineId ?? string.Empty;

        private DoctrineDefinition Def =>
            DoctrinesCatalog.TryGetDoctrine(_doctrineId, out var d) ? d : null;

        private DoctrineState StateValue =>
            string.IsNullOrEmpty(_doctrineId)
                ? DoctrineState.Locked
                : DoctrinesAPI.GetState(_doctrineId);

        private int ProgressValue =>
            string.IsNullOrEmpty(_doctrineId) ? 0 : DoctrinesAPI.GetProgress(_doctrineId);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Type Flags                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsDoctrine => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine, Global = true)]
        [DataSourceProperty]
        public override bool IsSelected => string.Equals(State.DoctrineId, _doctrineId);

        [DataSourceMethod]
        public override void ExecuteSelect()
        {
            if (!IsEnabled)
                return;

            State.DoctrineId = _doctrineId;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Enable                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public override bool IsEnabled => Def != null && StateValue != DoctrineState.Locked;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string Name => Def?.Name?.ToString() ?? string.Empty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          State                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StateText
        {
            get
            {
                return StateValue switch
                {
                    DoctrineState.Locked => L.S("doctrine_state_locked", "Locked"),
                    DoctrineState.InProgress => L.S("doctrine_state_in_progress", "In progress"),
                    DoctrineState.Unlocked => L.S("doctrine_state_unlocked", "Unlocked"),
                    DoctrineState.Acquired => L.S("doctrine_state_acquired", "Acquired"),
                    _ => string.Empty,
                };
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Progress                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool ShowProgress =>
            Def != null
            && Def.ProgressTarget > 0
            && StateValue != DoctrineState.Acquired
            && Settings.EnableFeatRequirements;

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string ProgressText =>
            Def == null
                ? string.Empty
                : $"{GetProgressPercent(ProgressValue, Def.ProgressTarget)}%";

        private static int GetProgressPercent(int progress, int target)
        {
            if (target <= 0)
                return 0;

            var p = progress < 0 ? 0 : progress;
            var t = target < 1 ? 1 : target;

            var v = (int)Math.Round(p * 100.0 / t);
            if (v < 0)
                return 0;
            if (v > 100)
                return 100;

            return v;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Icon                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StateIconSprite => ResolveStateIconSprite(StateValue);

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StateIconColor => ResolveStateIconColor(StateValue);

        private static string ResolveStateIconSprite(DoctrineState state)
        {
            // Keep using the same placeholder set as before (VM owns visuals).
            // Swap these later if you want custom doctrine sprites.
            return state switch
            {
                DoctrineState.Locked => "StdAssets\\lock_closed",
                DoctrineState.InProgress => "StdAssets\\lock_closed",
                DoctrineState.Unlocked => "StdAssets\\lock_opened",
                DoctrineState.Acquired => "General\\Icons\\icon_quest_done_checkmark",
                _ => string.Empty,
            };
        }

        private static string ResolveStateIconColor(DoctrineState state)
        {
            return state switch
            {
                DoctrineState.Acquired => "#268a7cff",
                _ => "#ffffffff",
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the sort value for the given sort key.
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
        /// Returns true if this row matches the given filter.
        /// </summary>
        internal override bool MatchesFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var comparison = StringComparison.OrdinalIgnoreCase;

            if (!string.IsNullOrEmpty(Name) && Name.IndexOf(filter, comparison) >= 0)
                return true;

            if (!string.IsNullOrEmpty(StateText) && StateText.IndexOf(filter, comparison) >= 0)
                return true;

            return false;
        }
    }
}
