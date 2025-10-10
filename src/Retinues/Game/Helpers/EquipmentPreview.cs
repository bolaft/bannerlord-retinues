using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Wrappers;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Helpers
{
    public static class EquipmentPreview
    {
        public static Equipment BuildStagedEquipment(WCharacter troop, WLoadout.Category category, int index = 0)
        {
            if (troop == null)
                return null;

            // start from the troop's current equipment (battle set by default)
            var src = troop.Loadout.Get(category, index);

            // clone so we don't mutate the real thing
            var eq = new Equipment(src.Base);

            // apply staged equip (if any)
            var beh = TroopEquipBehavior.Instance;
            var pending = beh?.GetPending(troop.StringId); // IEnumerable<PendingEquipData> in your base
            foreach (var p in pending)
            {
                if (p.Category != category || p.EquipmentIndex != index)
                    continue; // not for this set

                var item = MBObjectManager.Instance.GetObject<ItemObject>(p.ItemId);
                if (item != null)
                {
                    // simplest form; add modifier/amount if you track them
                    eq[p.Slot] = new EquipmentElement(item);
                }
            }

            return eq;
        }
    }
}
