using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Wrappers;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Helpers
{
    public static class EquipmentPreview
    {
        public static Equipment BuildStagedEquipment(WCharacter troop, int index = 0)
        {
            if (troop == null)
                return null;

            // start from the troop's current equipment (battle set by default)
            var src = troop.Loadout.Get(index);

            // clone so we don't mutate the real thing
            var eq = new Equipment(src.Base);

            // apply staged equip (if any)
            var pending = TroopEquipBehavior.GetAllStagedChanges(troop);
            if (pending == null)
                return eq; // nothing staged
            foreach (var p in pending)
            {
                if (p.EquipmentIndex != index)
                    continue; // not for this set

                var item = MBObjectManager.Instance.GetObject<ItemObject>(p.ItemId);
                if (item != null)
                    eq[p.Slot] = new EquipmentElement(item);
            }

            return eq;
        }
    }
}
