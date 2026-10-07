using System.Runtime.Serialization;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class SavedAmmoCompatibilityTests
    {
        [GameTest("NonSaleAmmoIsNotMistakenForCorruptSavedGear", "save-contracts", RequiresCampaign = false)]
        public static void NonSaleAmmoIsNotMistakenForCorruptSavedGear()
        {
            foreach (string id in new[] { "mod_elite_arrows", "mod_quest_bolts", "arrow_emp_1_a", "blunt_arrows" })
                Tests.AssertTrue(Ammo(id).IsValidEquipment,
                    "NotMerchandise alone must not remove legitimate saved or queued ammunition: " + id);
            foreach (string id in new[] { "ballista_projectile", "ballista_projectile_burning", "ballista_c_projectile",
                "ballista_c_projectile_burning", "burning_bolts", "tournament_arrows", "tournament_bolts" })
                Tests.AssertFalse(Ammo(id).IsValidEquipment, "Known mission ammunition still stays out of troop gear: " + id);
        }

        private static WItem Ammo(string id)
        {
            var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            item.StringId = id;
            typeof(ItemObject).GetProperty("NotMerchandise").GetSetMethod(true).Invoke(item, new object[] { true });
            item.AddWeapon(new WeaponComponentData(item, WeaponClass.Arrow, WeaponFlags.Consumable), null);
            var wrapped = new WItem(item);
            Tests.AssertTrue(wrapped.IsAmmo && wrapped.IsEquipment && item.NotMerchandise, "The fixture is real non-sale ammunition.");
            return wrapped;
        }

        [GameTest("OrdinaryThrowingStonesAreValidSavedGear", "save-contracts", RequiresCampaign = false)]
        public static void OrdinaryThrowingStonesAreValidSavedGear()
        {
            // Native looters carry these: WeaponClass.Stone is not a siege-only class.
            var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            item.StringId = "throwing_stone";
            item.AddWeapon(new WeaponComponentData(item, WeaponClass.Stone,
                WeaponFlags.RangedWeapon | WeaponFlags.Consumable), null);
            var wrapped = new WItem(item);
            Tests.AssertTrue(wrapped.IsEquipment, "The fixture represents an ordinary thrown weapon.");
            Tests.AssertTrue(wrapped.IsValidEquipment, "Loading a saved troop must preserve ordinary throwing stones.");
        }
    }
}
