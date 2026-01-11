using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers.Doctrines;
using Retinues.Editor.Events;
using Retinues.UI.Services;
using Retinues.UI.VM;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List.Doctrines
{
    /// <summary>
    /// Row representing a doctrine in the list.
    /// </summary>
    public sealed class DoctrinesListRowVM : BaseListRowVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly string _doctrineId;

        public DoctrinesListRowVM(ListHeaderVM header, string doctrineId)
            : base(header, doctrineId ?? string.Empty)
        {
            _doctrineId = doctrineId ?? string.Empty;
        }

        private DoctrinesController.DoctrineInfo Info =>
            DoctrinesController.GetDoctrineInfo(_doctrineId);

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
        public override bool IsEnabled =>
            Info != null && Info.State != DoctrinesController.DoctrineState.Locked;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Display                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string Name => Info?.Name ?? string.Empty;

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StateText => DoctrinesController.GetStateText(Info?.State);

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool ShowProgress =>
            Info != null
            && Info.Target > 0
            && Info.State != DoctrinesController.DoctrineState.Acquired;

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string ProgressText =>
            Info == null
                ? string.Empty
                : $"{DoctrinesController.GetProgressPercent(Info.Progress, Info.Target)}%";

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StateIconSprite => DoctrinesController.GetStateIconSprite(Info?.State);

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
