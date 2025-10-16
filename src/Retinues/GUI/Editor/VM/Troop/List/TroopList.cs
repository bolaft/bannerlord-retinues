using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.List
{
    [SafeClass]
    public sealed class TroopListVM : BaseList<TroopListVM, TroopRowVM>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly TroopScreenVM Screen;
        public readonly EditorVM Editor;

        public TroopListVM(TroopScreenVM screen)
        {
            Log.Info("Building TroopListVM...");

            Screen = screen;
            Editor = screen.Editor;
        }

        public void Initialize()
        {
            Log.Info("Initializing TroopListVM...");

            // Subscribe to events
            EventManager.TroopListChange.Register(BuildTroopList);
        }

        public void BuildTroopList()
        {
            // Clear existing lists
            RetinueTroops.Clear();
            EliteTroops.Clear();
            BasicTroops.Clear();
            MilitiaTroops.Clear();

            // Retinues
            foreach (
                var root in new[]
                {
                    Editor?.Faction?.RetinueElite,
                    Editor?.Faction?.RetinueBasic,
                }.Where(t => t?.Parent is null)
            )
                AddTroopTreeInOrder(root, RetinueTroops);

            // Elite troops
            foreach (
                var root in Editor?.Faction?.EliteTroops?.Where(t => t?.Parent is null)
                    ?? Enumerable.Empty<WCharacter>()
            )
                AddTroopTreeInOrder(root, EliteTroops);

            // Basic troops
            foreach (
                var root in Editor?.Faction?.BasicTroops?.Where(t => t?.Parent is null)
                    ?? Enumerable.Empty<WCharacter>()
            )
                AddTroopTreeInOrder(root, BasicTroops);

            // Militias
            foreach (
                var root in new[]
                {
                    Editor?.Faction?.MilitiaMelee,
                    Editor?.Faction?.MilitiaMeleeElite,
                    Editor?.Faction?.MilitiaRanged,
                    Editor?.Faction?.MilitiaRangedElite,
                }.Where(t => t?.Parent is null)
            )
                AddTroopTreeInOrder(root, MilitiaTroops);

            // Elite troops placeholder
            if (EliteTroops.Count == 0)
                EliteTroops.Add(new TroopRowVM(Editor?.TroopScreen, null));

            // Basic troops placeholder
            if (BasicTroops.Count == 0)
                BasicTroops.Add(new TroopRowVM(Editor?.TroopScreen, null));

            // Militia troops placeholder
            if (MilitiaTroops.Count == 0)
                MilitiaTroops.Add(new TroopRowVM(Editor?.TroopScreen, null));

            // Select the first row by default
            Select(Rows.FirstOrDefault());

            // Refresh lists at the very end to ensure UI updates
            OnPropertyChanged(nameof(RetinueTroops));
            OnPropertyChanged(nameof(EliteTroops));
            OnPropertyChanged(nameof(BasicTroops));
            OnPropertyChanged(nameof(MilitiaTroops));

            // Initialize rows
            foreach (var row in Rows)
                row.Initialize();
        }

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
                if (Editor?.Faction == Player.Kingdom)
                    return Player.IsFemale
                        ? L.S("queen_guard", "Queen's Guard")
                        : L.S("king_guard", "King's Guard");
                else
                    return L.S("retinue", "Retinue");
            }
        }

        [DataSourceProperty]
        public string MilitiaToggleText => L.S("list_toggle_militia", "Militia");

        /* ━━━━━━ Troop Lists ━━━━━ */

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> RetinueTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> EliteTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> BasicTroops { get; set; } = [];

        [DataSourceProperty]
        public MBBindingList<TroopRowVM> MilitiaTroops { get; set; } = [];

        [DataSourceProperty]
        public string SearchLabel => L.S("item_search_label", "Filter:");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override System.Collections.Generic.List<TroopRowVM> Rows =>
            [.. RetinueTroops.ToList(), .. EliteTroops.ToList(), .. BasicTroops.ToList(), .. MilitiaTroops.ToList()];

        public void Select(WCharacter troop)
        {
            Log.Info($"Selecting troop {troop} in TroopListVM.");
            Log.Trace();

            var row = Rows.FirstOrDefault(r => r.Troop == troop);

            if (row != null)
                Select(row);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void AddTroopTreeInOrder(WCharacter troop, MBBindingList<TroopRowVM> list)
        {
            if (troop == null || !troop.IsValid)
                return; // Ignore null or invalid troops

            // Create and add the row for this troop
            var row = new TroopRowVM(Editor?.TroopScreen, troop);
            list.Add(row);

            if (troop.IsRetinue || troop.IsMilitia)
                return; // Retinue and Militia troops do not have children

            // Find the troop's children
            var children = (Editor?.Faction?.BasicTroops ?? Enumerable.Empty<WCharacter>())
                .Concat(Editor?.Faction?.EliteTroops ?? Enumerable.Empty<WCharacter>())
                .Where(t => t?.Parent != null && t.Parent.Equals(troop));

            // Recursively add each child and its subtree
            foreach (var child in children)
                AddTroopTreeInOrder(child, list);
        }
    }
}
