using System.Diagnostics.Tracing;
using Retinues.Game.Doctrines.Definitions;
using Retinues.GUI.Components;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Pages.Doctrines.Controllers;
using Retinues.GUI.Editor.Shared.Views;
using Retinues.GUI.Services;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Modules.Pages.Doctrines.Views.Panel
{
    /// <summary>
    /// Doctrines panel.
    /// </summary>
    public sealed class DoctrinesPanelVM : BasePanelVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public DoctrinesPanelVM() => RefreshFeats();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Page)]
        [DataSourceProperty]
        public bool IsVisible => State.Page == EditorPage.Doctrines;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sprite                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string Sprite => $"General\\Perks\\{State.Doctrine.Sprite}";

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

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StatusText =>
            State.Doctrine.GetState() switch
            {
                Doctrine.State.Acquired => L.S("doctrine_status_acquired", "ACQUIRED"),
                Doctrine.State.Locked => L.S("doctrine_status_locked", "LOCKED"),
                _ => string.Empty,
            };

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool IsLocked => State.Doctrine.IsLocked;

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string LockedHint =>
            State.Doctrine.Previous != null
                ? L.T("doctrine_locked_hint", "{DOCTRINE} must be acquired first.")
                    .SetTextVariable("DOCTRINE", State.Doctrine.Previous.Name)
                    .ToString()
                : string.Empty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Costs                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool ShowCosts => ShowButton;

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
        public bool ShowFeats => _feats.Count > 0 && State.Doctrine.IsInProgress;

        [DataSourceProperty]
        public string FeatsHeaderText => L.S("doctrine_feats_header", "Feats");

        private readonly MBBindingList<FeatVM> _feats = [];

        [DataSourceProperty]
        public MBBindingList<FeatVM> Feats => _feats;

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

        static bool ShowButton =>
            (State.Doctrine.IsInProgress || State.Doctrine.IsUnlocked)
            && !State.Doctrine.IsAcquired;

        [DataSourceProperty]
        public Button<Doctrine> AcquireButton { get; } =
            new(
                action: DoctrinesController.Acquire,
                arg: () => State.Doctrine,
                refresh: [UIEvent.Doctrine],
                label: L.S("doctrine_acquire_button", "Acquire"),
                visibilityGate: () => ShowButton
            );
    }
}
