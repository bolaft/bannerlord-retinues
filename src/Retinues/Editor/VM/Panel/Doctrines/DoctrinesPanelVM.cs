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

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool HasSelection => State.Doctrine != null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Texts                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string NameText => State.Doctrine.Name.ToString();

        [DataSourceProperty]
        public string DescriptionHeaderText => L.S("doctrine_description_header", "Effects");

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string DescriptionText => State.Doctrine.Description.ToString();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Progress                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool IsAcquired => State.Doctrine.IsAcquired;

        [DataSourceProperty]
        public string AcquiredText => L.S("doctrine_acquired_text", "ACQUIRED");

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool ShowProgress =>
            State.Doctrine != null && !IsAcquired && Settings.EnableFeatRequirements;

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string ProgressText =>
            L.T("doctrine_progress_text", "Progress: {PROGRESS}%")
                .SetTextVariable("PROGRESS", State.Doctrine.Progress)
                .ToString();

        [EventListener(UIEvent.Doctrine)]
        public string ProgressTextColor =>
            State.Doctrine.GetState() switch
            {
                Doctrine.State.Acquired => "#ebaa49ff",
                Doctrine.State.Unlocked => "#ebaa49ff",
                _ => "#eec485ff",
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Costs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string CostsText
        {
            get
            {
                var gold = State.Doctrine.MoneyCost;
                var infl = State.Doctrine.InfluenceCost;

                if (gold > 0 && infl > 0)
                {
                    return L.T(
                            "doctrine_cost_text_both",
                            "Costs {GOLD} denars, {INFLUENCE} influence"
                        )
                        .SetTextVariable("GOLD", gold)
                        .SetTextVariable("INFLUENCE", infl)
                        .ToString();
                }
                else if (gold > 0)
                {
                    return L.T("doctrine_cost_text_gold", "Costs {GOLD} denars")
                        .SetTextVariable("GOLD", gold)
                        .ToString();
                }
                else if (infl > 0)
                {
                    return L.T("doctrine_cost_text_influence", "Costs {INFLUENCE} influence")
                        .SetTextVariable("INFLUENCE", infl)
                        .ToString();
                }
                else
                {
                    return string.Empty;
                }
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

            foreach (var feat in State.Doctrine.Feats)
                _feats.Add(new FeatVM(feat));

            OnPropertyChanged(nameof(ShowFeats));
            OnPropertyChanged(nameof(Feats));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Actions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<Doctrine> AcquireButton { get; } =
            new(
                action: DoctrinesController.Acquire,
                arg: () => State.Doctrine,
                refresh: [UIEvent.Doctrine],
                label: L.S("doctrine_acquire_button", "Acquire"),
                visibilityGate: () => !State.Doctrine.IsAcquired
            );
    }
}
