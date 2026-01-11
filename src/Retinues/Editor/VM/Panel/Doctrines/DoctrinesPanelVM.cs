using System.Diagnostics.Tracing;
using Retinues.Editor.Controllers.Doctrines;
using Retinues.Editor.Events;
using Retinues.UI.Services;
using Retinues.UI.VM;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Doctrines
{
    /// <summary>
    /// Doctrines panel.
    /// </summary>
    public sealed class DoctrinesPanelVM : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public DoctrinesPanelVM()
        {
            RefreshFeats();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible => EditorVM.Page == EditorPage.Doctrines;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private DoctrinesController.DoctrineInfo Selected =>
            DoctrinesController.GetDoctrineInfo(State.DoctrineId);

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool HasSelection => !string.IsNullOrEmpty(State.DoctrineId) && Selected != null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Texts                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string NameText => Selected?.Name ?? string.Empty;

        [DataSourceProperty]
        public string DescriptionHeaderText => L.S("doctrine_description_header", "Effects");

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string DescriptionText => Selected?.Description ?? string.Empty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Progress                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool ShowProgress => Selected != null && Selected.Target > 0;

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string ProgressText =>
            Selected == null
                ? string.Empty
                : L.T("doctrine_progress_text", "Progress: {PROGRESS}%")
                    .SetTextVariable(
                        "PROGRESS",
                        DoctrinesController.GetProgressPercent(Selected.Progress, Selected.Target)
                    )
                    .ToString();

        [EventListener(UIEvent.Doctrine)]
        public string ProgressTextColor =>
            Selected.State switch
            {
                DoctrinesController.DoctrineState.Acquired => "#f3c785ff",
                DoctrinesController.DoctrineState.Unlocked => "#f3c785ff",
                _ => "#F4E1C4FF",
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Costs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string CostsText =>
            Selected == null
                ? string.Empty
                : L.T("doctrine_cost_text", "Costs {GOLD} denars, {INFLUENCE} influence")
                    .SetTextVariable("GOLD", Selected.GoldCost)
                    .SetTextVariable("INFLUENCE", Selected.InfluenceCost)
                    .ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Feats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string FeatsHeaderText => L.S("doctrine_feats_header", "Feats");

        private readonly MBBindingList<FeatVM> _feats = [];

        [DataSourceProperty]
        public MBBindingList<FeatVM> Feats => _feats;

        [DataSourceProperty]
        public bool ShowFeats => _feats.Count > 0;

        [EventListener(UIEvent.Doctrine)]
        private void RefreshFeats()
        {
            _feats.Clear();

            var d = Selected;
            if (d?.Feats != null)
            {
                for (var i = 0; i < d.Feats.Length; i++)
                {
                    var f = d.Feats[i];
                    if (f == null)
                        continue;

                    _feats.Add(new FeatVM(f));
                }
            }

            OnPropertyChanged(nameof(ShowFeats));
            OnPropertyChanged(nameof(Feats));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Actions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<string> AcquireButton { get; } =
            new(
                action: DoctrinesController.Acquire,
                arg: () => State.DoctrineId,
                refresh: [UIEvent.Doctrine],
                label: L.S("doctrine_acquire_button", "Acquire")
            );
    }
}
