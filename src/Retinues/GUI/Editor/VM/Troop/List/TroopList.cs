using System.Collections.Generic;
using System.Linq;
using Retinues.Game;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.List
{
    /// <summary>
    /// ViewModel for grouped troop lists (retinue, elite, basic, militia).
    /// </summary>
    [SafeClass]
    public sealed class TroopListVM : BaseListVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new() { [UIEvent.Faction] = [nameof(RetinueToggleText)] };

        /// <summary>
        /// Rebuild list when the faction changes.
        /// </summary>
        protected override void OnFactionChange() => Build();

        /// <summary>
        /// Build troop group lists for the current faction and refresh the filter.
        /// </summary>
        public void Build()
        {
            RetinueTroops = [.. State.Faction.RetinueTroops.Select(t => new TroopRowVM(t))];
            EliteTroops = [.. State.Faction.EliteTroops.Select(t => new TroopRowVM(t))];
            BasicTroops = [.. State.Faction.BasicTroops.Select(t => new TroopRowVM(t))];
            MilitiaTroops = [.. State.Faction.MilitiaTroops.Select(t => new TroopRowVM(t))];
            CaravanTroops = [.. State.Faction.CaravanTroops.Select(t => new TroopRowVM(t))];
            VillagerTroops = [.. State.Faction.VillagerTroops.Select(t => new TroopRowVM(t))];
            CivilianTroops = [.. State.Faction.CivilianTroops.Select(t => new TroopRowVM(t))];
            BanditTroops = [.. State.Faction.BanditTroops.Select(t => new TroopRowVM(t))];

            if (EliteTroops.Count == 0 && !State.IsStudioMode)
                EliteTroops.Add(
                    new TroopRowVM(
                        null,
                        placeholderText: L.S(
                            "acquire_fief_to_unlock",
                            "Acquire a fief to unlock clan troops."
                        )
                    )
                ); // placeholder

            if (BasicTroops.Count == 0 && !State.IsStudioMode)
                BasicTroops.Add(
                    new TroopRowVM(
                        null,
                        placeholderText: L.S(
                            "acquire_fief_to_unlock",
                            "Acquire a fief to unlock clan troops."
                        )
                    )
                ); // placeholder

            if (MilitiaTroops.Count == 0 && !State.IsStudioMode)
                MilitiaTroops.Add(
                    new TroopRowVM(
                        null,
                        placeholderText: L.S(
                            "acquire_cultural_pride_to_unlock",
                            "Unlock with the Cultural Pride doctrine."
                        )
                    )
                ); // placeholder

            if (CaravanTroops.Count == 0 && !State.IsStudioMode)
                CaravanTroops.Add(
                    new TroopRowVM(
                        null,
                        placeholderText: L.S(
                            "royal_patronage_to_unlock",
                            "Unlock with the Royal Patronage doctrine."
                        )
                    )
                ); // placeholder

            if (VillagerTroops.Count == 0 && !State.IsStudioMode)
                VillagerTroops.Add(
                    new TroopRowVM(
                        null,
                        placeholderText: L.S(
                            "royal_patronage_to_unlock",
                            "Unlock with the Royal Patronage doctrine."
                        )
                    )
                ); // placeholder

            // Ensure visibility matches parent
            foreach (var r in Rows)
                r.IsVisible = IsVisible;

            OnPropertyChanged(nameof(RetinueTroops));
            OnPropertyChanged(nameof(EliteTroops));
            OnPropertyChanged(nameof(BasicTroops));
            OnPropertyChanged(nameof(MilitiaTroops));
            OnPropertyChanged(nameof(CaravanTroops));
            OnPropertyChanged(nameof(VillagerTroops));
            OnPropertyChanged(nameof(CivilianTroops));
            OnPropertyChanged(nameof(BanditTroops));
            OnPropertyChanged(nameof(ShowRetinueList));
            OnPropertyChanged(nameof(ShowMilitiaList));
            OnPropertyChanged(nameof(ShowCaravanList));
            OnPropertyChanged(nameof(ShowVillagerList));
            OnPropertyChanged(nameof(ShowCivilianList));
            OnPropertyChanged(nameof(ShowBanditList));

            RefreshFilter();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Components                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> RetinueTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> EliteTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> BasicTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> MilitiaTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> CaravanTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> VillagerTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> CivilianTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> BanditTroops { get; set; } = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━ Toggle Texts ━━━━━ */

        [DataSourceProperty]
        public string RegularToggleText => L.S("list_toggle_regular", "Regular");

        [DataSourceProperty]
        public string EliteToggleText => L.S("list_toggle_elite", "Elite");

        [DataSourceProperty]
        public string RetinueToggleText
        {
            get
            {
                if ((StringIdentifier)State.Faction == Player.Kingdom)
                    return Player.IsFemale
                        ? L.S("queen_guard", "Queen's Guard")
                        : L.S("king_guard", "King's Guard");
                else
                    return L.S("retinue", "Retinue");
            }
        }

        [DataSourceProperty]
        public string MilitiaToggleText => L.S("list_toggle_militia", "Militia");

        [DataSourceProperty]
        public string CaravanToggleText => L.S("list_toggle_caravan", "Caravans");

        [DataSourceProperty]
        public string VillagerToggleText => L.S("list_toggle_villager", "Villagers");

        [DataSourceProperty]
        public string CivilianToggleText => L.S("list_toggle_civilian", "Civilians");

        [DataSourceProperty]
        public string BanditToggleText => L.S("list_toggle_bandit", "Bandits");

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public bool ShowRetinueList => RetinueTroops.Count > 0 || !State.IsStudioMode;

        [DataSourceProperty]
        public bool ShowMilitiaList => MilitiaTroops.Count > 0 || !State.IsStudioMode;

        [DataSourceProperty]
        public bool ShowCaravanList => CaravanTroops.Count > 0 || !State.IsStudioMode;

        [DataSourceProperty]
        public bool ShowVillagerList => VillagerTroops.Count > 0 || !State.IsStudioMode;

        [DataSourceProperty]
        public bool ShowCivilianList => CivilianTroops.Count > 0 && State.IsStudioMode;

        [DataSourceProperty]
        public bool ShowBanditList => BanditTroops.Count > 0 && State.IsStudioMode;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Overrides                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override List<BaseListElementVM> Rows =>
            [
                .. RetinueTroops,
                .. EliteTroops,
                .. BasicTroops,
                .. MilitiaTroops,
                .. CaravanTroops,
                .. VillagerTroops,
                .. CivilianTroops,
                .. BanditTroops,
            ];

        /// <summary>
        /// Show the troop list and rebuild its contents.
        /// </summary>
        public override void Show()
        {
            base.Show();
            Build();
        }

        /// <summary>
        /// Hide the troop list and all child rows.
        /// </summary>
        public override void Hide()
        {
            foreach (var r in Rows)
                r.Hide();
            base.Hide();
        }
    }
}
