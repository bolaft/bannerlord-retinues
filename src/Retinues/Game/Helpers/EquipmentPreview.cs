using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Wrappers;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Helpers
{
    public static class EquipmentPreview
    {
        public static Equipment BuildStagedEquipment(WCharacter troop, Equipment equipment = null)
        {
            if (troop == null)
                return null;

            // start from the troop's current equipment (battle set by default)
            var src = equipment ?? troop.Base?.Equipment;
            if (src == null)
                return null;

            // clone so we don't mutate the real thing
            var eq = new Equipment(src);

            // apply staged equip (if any)
            var beh = TroopEquipBehavior.Instance;
            var pending = beh?.GetPending(troop.StringId); // IEnumerable<PendingEquipData> in your base
            if (pending != null)
            {
                foreach (var p in pending)
                {
                    // p.Slot is EquipmentIndex; p.ItemId may be null for unequip
                    if (string.IsNullOrEmpty(p.ItemId))
                    {
                        // unequip
                        eq[p.Slot] = default; // clears the slot
                    }
                    else
                    {
                        var item = MBObjectManager.Instance.GetObject<ItemObject>(p.ItemId);
                        if (item != null)
                        {
                            // simplest form; add modifier/amount if you track them
                            eq[p.Slot] = new EquipmentElement(item);
                        }
                    }
                }
            }

            return eq;
        }

        public static CharacterCode BuildPreviewCode(WCharacter troop)
        {
            if (troop?.Base == null)
                return default;
            var baseEq = troop.Equipment.Base;
            var eq = BuildStagedEquipment(troop, baseEq);
            if (eq == null)
                return CharacterCode.CreateFrom(troop.Base); // fallback

            return CharacterCode.CreateFrom(troop.Base, eq);
        }

#if BL13
        public static CharacterImageIdentifierVM BuildPreviewImageIdentifier(WCharacter troop) =>
            new(BuildPreviewCode(troop));
#else
        public static ImageIdentifierVM BuildPreviewImageIdentifier(WCharacter troop) =>
            new(BuildPreviewCode(troop));
#endif
    }
}
