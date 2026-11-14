using System;
using System.Runtime.Serialization;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class FormationPersistenceTests
    {
        [GameTest("RangedTroopKeepsInfantryOverrideAfterReload", "save-contracts", RequiresCampaign = false)]
        public static void RangedTroopKeepsInfantryOverrideAfterReload()
        {
            using var fixture = new Europe1700RecruitmentTests.Fixture();
            var bow = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            bow.StringId = ""; // Do not add test items to the shared wrapper cache.
            bow.AddWeapon(new WeaponComponentData(bow, WeaponClass.Bow, WeaponFlags.RangedWeapon), null);
            WCharacter Troop()
            {
                var character = fixture.Troop("formation_" + Guid.NewGuid().ToString("N"));
                character.Equipment[EquipmentIndex.Weapon0] = new EquipmentElement(bow);
                return new WCharacter(character);
            }

            var troop = Troop();
            troop.UpdateFormationClass();
            Tests.AssertEqual(FormationClass.Ranged, troop.FormationClass, "The bow would normally classify this troop as ranged.");
            troop.FormationClassOverride = FormationClass.Infantry;
            string saved = troop.Serialize();
            Tests.AssertTrue(saved.Contains("FormationClassOverrideAttribute"));
            for (int load = 0; load < 3; load++)
            {
                troop = Troop();
                troop.Deserialize(saved);
                troop.UpdateFormationClass(); // Also recalculate after the equipment has been restored.
                Tests.AssertEqual(FormationClass.Infantry, troop.FormationClassOverride);
                Tests.AssertEqual(FormationClass.Infantry, troop.FormationClass);
                Tests.AssertEqual((int)FormationClass.Infantry, troop.FormationGroup);
                Tests.AssertFalse(troop.IsRanged);
                Tests.AssertFalse(troop.IsMounted);
                saved = troop.Serialize();
            }
            troop.FormationClassOverride = FormationClass.Unset;
            Tests.AssertEqual(FormationClass.Ranged, troop.FormationClass, "Auto still derives the formation from the equipment.");
        }
    }
}
