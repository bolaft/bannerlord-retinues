using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Compatibility;
using Retinues.Game.Wrappers;
using Retinues.Managers;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class DismembermentCompatibilityTests
    {
        private static WItem Armor(string id, ItemObject.ItemTypeEnum type, bool nonSale = true)
        {
            var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            item.StringId = id;
            item.Type = type;
            AccessTools.PropertySetter(typeof(ItemObject), "NotMerchandise").Invoke(item, new object[] { nonSale });
            return new WItem(item);
        }

        [GameTest("DismembermentArmorCannotBeSelectedOrPasted", "equipment", RequiresCampaign = false)]
        public static void DismembermentArmorCannotBeSelectedOrPasted()
        {
            var cases = new[] {
                ("dp_long_desert_robe", ItemObject.ItemTypeEnum.BodyArmor),
                ("DP_guarded_padded_vambrace_l", ItemObject.ItemTypeEnum.HandArmor),
                ("dp_southern_moccasins_r", ItemObject.ItemTypeEnum.LegArmor),
                ("dP_test_cape", ItemObject.ItemTypeEnum.Cape),
            };
            foreach (var (id, type) in cases)
            {
                var item = Armor(id, type);
                Tests.AssertTrue(item.IsDismembermentVisual, "Editor must exclude this visual asset: " + id);
                Tests.AssertFalse(EquipmentManager.IsUnlockedForPaste(null, item.Slots[0], item),
                    "Paste must reject visual assets before unlock-all or studio overrides.");
                Tests.AssertFalse(EquipmentManager.CanEquip(null, item, out var reason), "Cannot equip a visual asset.");
                Tests.AssertEqual(EquipmentManager.EquipLimitReason.VisualAsset, reason,
                    "Visual assets are rejected before troop/config/cost processing.");
                Tests.AssertEqual(id, item.Base.StringId, "Filtering does not rewrite saved item identities.");
            }
        }

        [GameTest("DismembermentFilterPreservesOrdinaryModArmor", "equipment", RequiresCampaign = false)]
        public static void DismembermentFilterPreservesOrdinaryModArmor()
        {
            foreach (var id in new[] { "mod_quest_armor", "mod_dp_armor", "dplate_armor" })
                Tests.AssertFalse(Armor(id, ItemObject.ItemTypeEnum.BodyArmor).IsDismembermentVisual,
                    "Legitimate non-sale armor remains available: " + id);
            Tests.AssertFalse(Armor("dp_merchandise", ItemObject.ItemTypeEnum.BodyArmor, false).IsDismembermentVisual,
                "A saleable item merely sharing the prefix is not a dismemberment asset.");
            Tests.AssertFalse(EquipmentCompatibility.IsDismembermentVisual(null), "No item is not a visual asset.");
        }
    }
}
