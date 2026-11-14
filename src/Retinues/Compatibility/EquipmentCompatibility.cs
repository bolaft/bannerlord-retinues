using System;
using TaleWorlds.Core;

namespace Retinues.Compatibility
{
    internal static class EquipmentCompatibility
    {
        // DismembermentPlus registers partial armor meshes as non-sale dp_ items.
        // Keep them registered for its battle visuals, but never offer them as troop gear.
        // Non-sale items alone are not a reason to exclude legitimate modded equipment.
        internal static bool IsDismembermentVisual(ItemObject item)
        {
            if (item == null || !item.NotMerchandise
                || item.StringId?.StartsWith("dp_", StringComparison.OrdinalIgnoreCase) != true)
                return false;

            return item.ItemType == ItemObject.ItemTypeEnum.BodyArmor
                || item.ItemType == ItemObject.ItemTypeEnum.HandArmor
                || item.ItemType == ItemObject.ItemTypeEnum.LegArmor
                || item.ItemType == ItemObject.ItemTypeEnum.Cape;
        }
    }
}
