using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Behaviors.Staging;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class EquipmentSaveCompatibilityTests
    {
        private static MEquipment Equipment()
        {
            // Private, managed objects: never register fixtures in the live campaign/cache.
            var character = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
            character.StringId = "";
            var owner = new WCharacter(character);
            var rosterBase = (MBEquipmentRoster)FormatterServices.GetUninitializedObject(typeof(MBEquipmentRoster));
            var roster = new MEquipmentRoster(rosterBase, owner);
            AccessTools.Field(typeof(WCharacter), "_equipmentRosterCache").SetValue(owner, roster);
            AccessTools.Field(typeof(MEquipmentRoster), "_equipmentsCache").SetValue(roster, new List<MEquipment>());
            return new MEquipment(new TaleWorlds.Core.Equipment(), owner);
        }

        private static WItem Weapon(WeaponClass weaponClass)
        {
            var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            item.StringId = ""; // Avoid global wrapper cache changes in an in-game run.
            item.AddWeapon(new WeaponComponentData(item, weaponClass, (WeaponFlags)0), null);
            return new WItem(item);
        }

        [GameTest("SavedGearIsNotDeletedByEditorVisibilityRules", "save-contracts", RequiresCampaign = false)]
        public static void SavedGearIsNotDeletedByEditorVisibilityRules()
        {
            var banner = Weapon(WeaponClass.Banner);
            Tests.AssertFalse(banner.IsValidEquipment, "The fixture is hidden in the equipment editor.");
            var equipment = Equipment();
            equipment.Base[EquipmentIndex.Weapon0] = new EquipmentElement(banner.Base);
            equipment.ItemsStaging = new List<string> { "0|banner_from_other_mod", "1|" };
            equipment.ItemStagingProgress = 5.25f;
            Tests.AssertEqual(0, equipment.ScrubInvalidItems(_ => banner));
            Tests.AssertTrue(ReferenceEquals(banner.Base, equipment.Base[EquipmentIndex.Weapon0].Item));
            Tests.AssertEqual(2, equipment.ItemsStaging.Count);
            Tests.AssertEqual(5.25f, equipment.ItemStagingProgress);
        }

        [GameTest("MissingQueuedItemKeepsSavedOrderAndProgress", "save-contracts", RequiresCampaign = false)]
        public static void MissingQueuedItemKeepsSavedOrderAndProgress()
        {
            var equipment = Equipment();
            var queue = new List<string> { "0|temporarily_unresolved_save_audit_item", "1|" };
            equipment.ItemsStaging = queue;
            equipment.ItemStagingProgress = 5.25f;
            Tests.AssertEqual(0, equipment.ScrubInvalidItems(_ => null));
            Tests.AssertTrue(float.IsPositiveInfinity(equipment.GetNextStagedHours(1f)));
            Tests.AssertFalse(equipment.TryApplyNextStagedItem(out _, out _, out _));
            Tests.AssertFalse(StagedItemProgressBehavior.AdvanceEquipment(equipment, 9f, 1f));
            Tests.AssertTrue(queue.SequenceEqual(equipment.ItemsStaging));
            Tests.AssertEqual(5.25f, equipment.ItemStagingProgress);
            for (int load = 0; load < 3; load++)
            {
                string saved = equipment.Serialize();
                Tests.AssertTrue(!string.IsNullOrEmpty(saved));
                equipment = Equipment();
                equipment.Deserialize(saved);
                Tests.AssertTrue(queue.SequenceEqual(equipment.ItemsStaging));
                Tests.AssertEqual(5.25f, equipment.ItemStagingProgress);
            }
        }

        [GameTest("SiegeCleanupKeepsPendingReplacementAndProgress", "save-contracts", RequiresCampaign = false)]
        public static void SiegeCleanupKeepsPendingReplacementAndProgress()
        {
            var siege = Weapon(WeaponClass.Boulder);
            var stone = Weapon(WeaponClass.Stone);
            var equipment = Equipment();
            equipment.Base[EquipmentIndex.Weapon0] = new EquipmentElement(siege.Base);
            equipment.ItemsStaging = new List<string> { "0|normal_stone", "1|siege", "2|unresolved" };
            equipment.ItemStagingProgress = 5.25f;
            Tests.AssertEqual(2, equipment.ScrubInvalidItems(id => id == "normal_stone" ? stone : id == "siege" ? siege : null));
            Tests.AssertTrue(equipment.Base[EquipmentIndex.Weapon0].IsEmpty);
            Tests.AssertTrue(equipment.ItemsStaging.SequenceEqual(new[] { "0|normal_stone", "2|unresolved" }));
            Tests.AssertEqual(5.25f, equipment.ItemStagingProgress);
        }
    }
}
