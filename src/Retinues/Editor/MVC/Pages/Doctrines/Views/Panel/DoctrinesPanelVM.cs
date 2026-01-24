using System.Diagnostics.Tracing;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Doctrines.Controllers;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Doctrines.Views.Panel
{
    /// <summary>
    /// Doctrines panel.
    /// </summary>
    public sealed class DoctrinesPanelVM : BasePanelVM
    {
        private static Doctrine CurrentDoctrine => State?.Doctrine;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public DoctrinesPanelVM() => RefreshFeats();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool OnDoctrinesPage => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sprite                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string Sprite =>
            CurrentDoctrine != null ? $"General\\Perks\\{CurrentDoctrine.Sprite}" : string.Empty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Texts                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string NameText => CurrentDoctrine?.Name?.ToString() ?? string.Empty;

        [DataSourceProperty]
        public string DescriptionHeaderText => L.S("doctrine_description_header", "Effects");

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string DescriptionText => CurrentDoctrine?.Description?.ToString() ?? string.Empty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Progress                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string ProgressText =>
            CurrentDoctrine != null
                ? L.T("doctrine_progress_text", "Progress: {PROGRESS}%")
                    .SetTextVariable("PROGRESS", CurrentDoctrine.Progress)
                    .ToString()
                : string.Empty;

        [EventListener(UIEvent.Doctrine)]
        public string ProgressTextColor =>
            CurrentDoctrine == null
                ? "#eec485ff"
                : CurrentDoctrine.GetState() switch
                {
                    Doctrine.State.Acquired => "#ebaa49ff",
                    Doctrine.State.Unlocked => "#ebaa49ff",
                    _ => "#eec485ff",
                };

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StatusText =>
            CurrentDoctrine == null
                ? string.Empty
                : CurrentDoctrine.GetState() switch
                {
                    Doctrine.State.Acquired => L.S("doctrine_status_acquired", "ACQUIRED"),
                    Doctrine.State.Locked => L.S("doctrine_status_locked", "LOCKED"),
                    Doctrine.State.Overridden => L.S("doctrine_status_overridden", "OVERRIDDEN"),
                    _ => string.Empty,
                };

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public bool IsLocked => CurrentDoctrine?.IsLocked ?? false;

        [EventListener(UIEvent.Doctrine)]
        [DataSourceProperty]
        public string StatusHint =>
            CurrentDoctrine == null
                ? string.Empty
                : CurrentDoctrine.GetState() switch
                {
                    Doctrine.State.Locked => CurrentDoctrine.Prerequisite != null
                        ? L.T("doctrine_locked_hint", "{DOCTRINE} must be acquired first.")
                            .SetTextVariable("DOCTRINE", CurrentDoctrine.Prerequisite.Name)
                            .ToString()
                        : string.Empty,
                    Doctrine.State.Overridden => CurrentDoctrine.OverriddenHint.ToString(),
                    _ => string.Empty,
                };

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
        public bool ShowFeats => _feats.Count > 0 && (CurrentDoctrine?.IsInProgress ?? false);

        [DataSourceProperty]
        public string FeatsHeaderText => L.S("doctrine_feats_header", "Feats");

        private readonly MBBindingList<FeatVM> _feats = [];

        [DataSourceProperty]
        public MBBindingList<FeatVM> Feats => _feats;

        [EventListener(UIEvent.Doctrine, UIEvent.Page)]
        private void RefreshFeats()
        {
            _feats.Clear();

            var doctrine = CurrentDoctrine;
            if (doctrine?.Feats == null)
            {
                OnPropertyChanged(nameof(ShowFeats));
                OnPropertyChanged(nameof(Feats));
                return;
            }

            foreach (var feat in doctrine.Feats)
                _feats.Add(new FeatVM(feat));

            OnPropertyChanged(nameof(ShowFeats));
            OnPropertyChanged(nameof(Feats));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Actions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static bool ShowButton =>
            CurrentDoctrine != null
            && (CurrentDoctrine.IsInProgress || CurrentDoctrine.IsUnlocked)
            && !CurrentDoctrine.IsAcquired;

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
