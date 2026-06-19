using System.Linq;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for WItem.GetEquipmentsForSlot / the EquipmentsBySlot cache that backs the editor's
    /// equipment list: items are bucketed only into slots they actually fit.
    /// </summary>
    public static class EligibilityTests
    {
        [GameTest(
            "EquipmentsForSlotFitTheSlot",
            "eligibility",
            "Every item bucketed for a slot can be equipped in it"
        )]
        public static void EquipmentsForSlotFitTheSlot(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var body = WItem.GetEquipmentsForSlot(EquipmentIndex.Body);
            Tests.AssertNotNull(body, "GetEquipmentsForSlot returned a list.");
            Tests.AssertTrue(body.Count > 0, "Body slot has at least one item.");

            foreach (var item in body)
                Tests.AssertTrue(
                    item != null && item.IsEquippableInSlot(EquipmentIndex.Body),
                    $"Bucketed item fits the Body slot: {item?.StringId}"
                );
        }

        [GameTest(
            "ItemBucketedOnlyForFittingSlots",
            "eligibility",
            "A body item appears in the Body bucket but not a weapon bucket"
        )]
        public static void ItemBucketedOnlyForFittingSlots(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var bodyItem = WItem.GetEquipmentsForSlot(EquipmentIndex.Body)
                .FirstOrDefault(i => i != null && !i.IsEquippableInSlot(EquipmentIndex.Weapon0));
            Tests.AssertNotNull(bodyItem, "A body-only item is available.");
            var id = bodyItem.StringId;

            Tests.AssertTrue(
                WItem.GetEquipmentsForSlot(EquipmentIndex.Body).Any(i => i?.StringId == id),
                "The body item is bucketed for the Body slot."
            );
            Tests.AssertFalse(
                WItem.GetEquipmentsForSlot(EquipmentIndex.Weapon0).Any(i => i?.StringId == id),
                "The body item is not bucketed for a weapon slot it cannot occupy."
            );
        }
    }
}
