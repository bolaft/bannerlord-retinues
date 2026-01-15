using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Editor.Events;
using Retinues.Game.Doctrines.Definitions;
using Retinues.UI.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List.Doctrines
{
    /// <summary>
    /// Row representing a doctrine in the list.
    /// </summary>
    public sealed class DoctrinesListRowVM(ListHeaderVM header, Doctrine doctrine)
        : BaseListRowVM(header, doctrine.Id)
    {
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
        public override bool IsSelected => State.Doctrine.Id == doctrine.Id;

        [DataSourceMethod]
        public override void ExecuteSelect()
        {
            if (!IsEnabled)
                return;

            State.Doctrine = doctrine;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string Name => doctrine.Name.ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          State                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StateText
        {
            get
            {
                return doctrine.GetState() switch
                {
                    Doctrine.State.Locked => L.S("doctrine_state_locked", "Locked"),
                    Doctrine.State.InProgress => L.S("doctrine_state_in_progress", "In progress"),
                    Doctrine.State.Unlocked => L.S("doctrine_state_unlocked", "Unlocked"),
                    Doctrine.State.Acquired => L.S("doctrine_state_acquired", "Acquired"),
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
            doctrine.GetState() != Doctrine.State.Acquired && Settings.EnableFeatRequirements;

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string ProgressText => $"{doctrine.Progress}%";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Icon                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StateIconSprite =>
            doctrine.GetState() switch
            {
                Doctrine.State.Locked => "StdAssets\\lock_closed",
                Doctrine.State.InProgress => "StdAssets\\lock_opened",
                Doctrine.State.Unlocked => "StdAssets\\lock_opened",
                Doctrine.State.Acquired => "General\\Icons\\icon_quest_done_checkmark",
                _ => string.Empty,
            };

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StateIconColor =>
            doctrine.GetState() switch
            {
                Doctrine.State.Acquired => "#268a7cff",
                _ => "#ffffffff",
            };

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

            return false;
        }
    }
}
