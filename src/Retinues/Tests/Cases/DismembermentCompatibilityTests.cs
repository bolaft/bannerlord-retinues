using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Compatibility;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.MVC.Pages.Equipment.Services;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class DismembermentCompatibilityTests
    {
        private sealed class FixtureItem : WItem
        {
            private readonly ItemObject _fixture;
            public FixtureItem(ItemObject item) : base(item) => _fixture = item;
            public override ItemObject Base => _fixture;
        }

        private static WItem Armor(string id, ItemObject.ItemTypeEnum type, bool nonSale = true)
        {
            var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            item.StringId = id;
            item.Type = type;
            AccessTools.PropertySetter(typeof(ItemObject), "NotMerchandise").Invoke(item, new object[] { nonSale });
            AccessTools.PropertySetter(typeof(ItemObject), "ItemComponent").Invoke(item,
                new[] { FormatterServices.GetUninitializedObject(typeof(ArmorComponent)) });
            return new FixtureItem(item);
        }

        [GameTest("DismembermentArmorCannotBeSelectedOrPasted", "equipment", RequiresCampaign = false)]
        public static void DismembermentArmorCannotBeSelectedOrPasted()
        {
            // Real asset families in DismembermentPlus 2.0.8.8; matching uses IDs, not display names.
            var cases = new[] {
                ("dp_long_desert_robe", ItemObject.ItemTypeEnum.BodyArmor),
                ("DP_guarded_padded_vambrace_l", ItemObject.ItemTypeEnum.HandArmor),
                ("dp_southern_moccasins_r", ItemObject.ItemTypeEnum.LegArmor),
                ("dP_test_cape", ItemObject.ItemTypeEnum.Cape),
            };
            foreach (var (id, type) in cases)
            {
                var item = Armor(id, type);
                Tests.AssertTrue(EquipmentCompatibility.IsDismembermentVisual(item.Base), id);
                Tests.AssertFalse(item.IsValidEquipment, "Must not enter editor/random/workshop pools: " + id);
                var decision = EquipRules.CanSetItem(default, null, item.Slots[0], item);
                Tests.AssertEqual(EquipSkipReason.Incompatible, decision.Reason, "Paste must not bypass the exclusion.");
                Tests.AssertFalse(item.IsUnsafeForTroopEquipment, "Visibility must not turn into destructive saved-equipment cleanup.");
            }
        }

        [GameTest("DismembermentFilterPreservesOrdinaryModArmor", "equipment", RequiresCampaign = false)]
        public static void DismembermentFilterPreservesOrdinaryModArmor()
        {
            foreach (var id in new[] { "mod_quest_armor", "mod_dp_armor", "dplate_armor" })
                Tests.AssertTrue(Armor(id, ItemObject.ItemTypeEnum.BodyArmor).IsValidEquipment,
                    "Legitimate non-sale armor remains selectable: " + id);
            Tests.AssertTrue(Armor("dp_merchandise", ItemObject.ItemTypeEnum.BodyArmor, false).IsValidEquipment,
                "A saleable item merely sharing the prefix is not a dismemberment asset.");
            Tests.AssertFalse(EquipmentCompatibility.IsDismembermentVisual(null), "No item is not a visual asset.");
            Tests.AssertEqual(0, Armor("body_male_a_d", ItemObject.ItemTypeEnum.Invalid).Slots.Count,
                "Dismemberment body-part assets have no wearable equipment slot.");
        }
    }
}
