using System;
using System.Linq;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class TroopListVM : ViewModel
    {
        private readonly ClanManagementMixinVM _owner;

        public TroopListVM(ClanManagementMixinVM owner) => _owner = owner;

        [DataSourceProperty] public MBBindingList<TroopRowVM> BasicTroops { get; } = new();

        [DataSourceProperty] public MBBindingList<TroopRowVM> EliteTroops { get; } = new();

        public void Refresh()
        {
            var prev = _owner.SelectedRow?.Troop?.StringId;

            BasicTroops.Clear();
            foreach (var root in TroopManager.BasicCustomTroops.Where(t => t.Parent == null))
                AddTroopWithChildren(root, BasicTroops, 0);

            EliteTroops.Clear();
            foreach (var root in TroopManager.EliteCustomTroops.Where(t => t.Parent == null))
                AddTroopWithChildren(root, EliteTroops, 0);

            if (!string.IsNullOrEmpty(prev))
            {
                var found = BasicTroops.Concat(EliteTroops).FirstOrDefault(r => r.Troop.StringId == prev);
                if (found != null)
                    _owner.HandleRowSelected(found);
            }

            OnPropertyChanged(nameof(BasicTroops));
            OnPropertyChanged(nameof(EliteTroops));

            if (_owner.SelectedRow == null)
                _owner.SelectFirstTroop();

            Log.Info($"TroopList.Refresh: Basic={BasicTroops.Count}, Elite={EliteTroops.Count}");
        }

        private void AddTroopWithChildren(CharacterWrapper troop, MBBindingList<TroopRowVM> list, int depth)
        {
            var row = new TroopRowVM(troop, _owner.HandleRowSelected) { Depth = depth };
            list.Add(row);

            var children = TroopManager.BasicCustomTroops
                .Concat(TroopManager.EliteCustomTroops)
                .Where(t => t.Parent == troop);

            foreach (var child in children)
                AddTroopWithChildren(child, list, depth + 1);
        }

        public void RefreshRows()
        {
            foreach (var r in BasicTroops) r.Refresh();
            foreach (var r in EliteTroops) r.Refresh();
        }

        public bool IsEliteSelectedContext(TroopRowVM row)
        {
            if (row == null) return false;
            return EliteTroops.Contains(row);
        }
    }
}
