using System.Diagnostics.Tracing;
using Retinues.Configuration;
using Retinues.Editor.Controllers.Doctrines;
using Retinues.Editor.Events;
using Retinues.Game.Doctrines;
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

        private DoctrineDefinition Selected
        {
            get
            {
                if (string.IsNullOrEmpty(State.DoctrineId))
                    return null;

                return DoctrinesCatalog.TryGetDoctrine(State.DoctrineId, out var d) ? d : null;
            }
        }

        private DoctrineState SelectedState =>
            Selected == null ? DoctrineState.Locked : DoctrinesAPI.GetState(Selected.Id);

        private int SelectedProgress =>
            Selected == null ? 0 : DoctrinesAPI.GetProgress(Selected.Id);

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool HasSelection => Selected != null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Texts                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string NameText => Selected?.Name?.ToString() ?? string.Empty;

        [DataSourceProperty]
        public string DescriptionHeaderText => L.S("doctrine_description_header", "Effects");

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string DescriptionText => Selected?.Description?.ToString() ?? string.Empty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Progress                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool IsAcquired => SelectedState == DoctrineState.Acquired;

        [DataSourceProperty]
        public string AcquiredText => L.S("doctrine_acquired_text", "ACQUIRED");

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool ShowProgress =>
            Selected != null
            && Selected.ProgressTarget > 0
            && !IsAcquired
            && Settings.EnableFeatRequirements;

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string ProgressText
        {
            get
            {
                if (Selected == null)
                    return string.Empty;

                var pct = GetProgressPercent(SelectedProgress, Selected.ProgressTarget);

                return L.T("doctrine_progress_text", "Progress: {PROGRESS}%")
                    .SetTextVariable("PROGRESS", pct)
                    .ToString();
            }
        }

        [EventListener(UIEvent.Doctrine)]
        public string ProgressTextColor =>
            SelectedState switch
            {
                DoctrineState.Acquired => "#ebaa49ff",
                DoctrineState.Unlocked => "#ebaa49ff",
                _ => "#eec485ff",
            };

        private static int GetProgressPercent(int progress, int target)
        {
            if (target <= 0)
                return 0;

            var p = progress < 0 ? 0 : progress;
            var t = target < 1 ? 1 : target;

            var v = (int)System.Math.Round(p * 100.0 / t);
            if (v < 0)
                return 0;
            if (v > 100)
                return 100;

            return v;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Costs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string CostsText
        {
            get
            {
                if (Selected == null)
                    return string.Empty;

                var gold = DoctrinesController.GetGoldCost(Selected);
                var inf = DoctrinesController.GetInfluenceCost(Selected);

                if (gold <= 0 && inf <= 0)
                    return string.Empty;

                if (gold > 0 && inf > 0)
                {
                    return L.T(
                            "doctrine_cost_text_both",
                            "Costs {GOLD} denars, {INFLUENCE} influence"
                        )
                        .SetTextVariable("GOLD", gold)
                        .SetTextVariable("INFLUENCE", inf)
                        .ToString();
                }

                if (gold > 0)
                {
                    return L.T("doctrine_cost_text_gold", "Costs {GOLD} denars")
                        .SetTextVariable("GOLD", gold)
                        .ToString();
                }

                return L.T("doctrine_cost_text_influence", "Costs {INFLUENCE} influence")
                    .SetTextVariable("INFLUENCE", inf)
                    .ToString();
            }
        }

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

        [EventListener(UIEvent.Doctrine, UIEvent.Page)]
        private void RefreshFeats()
        {
            _feats.Clear();

            var d = Selected;
            if (d?.Feats != null)
            {
                for (var i = 0; i < d.Feats.Count; i++)
                {
                    var link = d.Feats[i];

                    if (string.IsNullOrEmpty(link.FeatId))
                        continue;

                    if (
                        !DoctrinesCatalog.TryGetFeat(link.FeatId, out var featDef)
                        || featDef == null
                    )
                        continue;

                    _feats.Add(new FeatVM(link, featDef));
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
                label: L.S("doctrine_acquire_button", "Acquire"),
                visibilityGate: () => !DoctrinesAPI.IsAcquired(State.DoctrineId)
            );
    }
}
