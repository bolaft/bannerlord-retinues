using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor;
using Retinues.Editor.MVC.Pages.Equipment.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    public static class EquipmentHolsterTests
    {
        private static ItemObject Weapon(string[] holsters)
        {
            var item = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            item.StringId = ""; // Private fixture: never resolve a real campaign item.
            item.Type = ItemObject.ItemTypeEnum.OneHandedWeapon;
            item.AddWeapon(new WeaponComponentData(item, WeaponClass.OneHandedSword, WeaponFlags.MeleeWeapon), null);
            AccessTools.PropertySetter(typeof(ItemObject), "ItemHolsters").Invoke(item, new object[] { holsters });
            return item;
        }

        // Also used by the isolated host's object-registration/queue continuation check.
        internal static MEquipment Equipment()
        {
            var character = (CharacterObject)FormatterServices.GetUninitializedObject(typeof(CharacterObject));
            character.StringId = "";
            var owner = new WCharacter(character);
            var rosterBase = (MBEquipmentRoster)FormatterServices.GetUninitializedObject(typeof(MBEquipmentRoster));
            var roster = new MEquipmentRoster(rosterBase, owner);
            AccessTools.Field(typeof(WCharacter), "_equipmentRosterCache").SetValue(owner, roster);
            AccessTools.Field(typeof(MEquipmentRoster), "_equipmentsCache").SetValue(roster, new List<MEquipment>());
            return new MEquipment(new Equipment(), owner);
        }

        [GameTest("MalformedHolstersExcludedFromNewGear", "equipment", RequiresCampaign = false)]
        public static void MalformedHolstersExcludedFromNewGear()
        {
            foreach (var holsters in new string[][] { null, new string[4], new[] { "right", null }, new[] { "", "", "", "", "" } })
            {
                var item = new WItem(Weapon(holsters));
                Tests.AssertFalse(item.IsValidEquipment);
                Tests.AssertFalse(EquipRules.CanSetItem(new EquipContext(EditorMode.Universal, true, null, null),
                    _ => null, EquipmentIndex.Weapon0, item).Allowed, "Direct editor actions must also reject stale list entries.");
            }
            Tests.AssertTrue(new WItem(Weapon(new string[0])).IsValidEquipment);
            Tests.AssertTrue(new WItem(Weapon(new[] { "" })).IsValidEquipment);
            var armor = (ItemObject)FormatterServices.GetUninitializedObject(typeof(ItemObject));
            Tests.AssertFalse(ItemHolsterSafety.HasInvalidWeaponHolsters(armor), "Non-weapons may have no holster array.");
        }

        [GameTest("HolsterFallbackPreservesValidEntriesAndSharedDefinition", "equipment", RequiresCampaign = false)]
        public static void HolsterFallbackPreservesValidEntriesAndSharedDefinition()
        {
            var holsters = new[] { "right", null, "", "left" };
            var item = Weapon(holsters);
            var requested = new List<string>();
            Tests.AssertTrue(ItemHolsterSafety.TryGetFallbackIndices(item, name =>
            {
                requested.Add(name);
                return name == "right" ? 21 : 34;
            }, out var indices));
            Tests.AssertEqual(21, indices[0]);
            Tests.AssertEqual(-1, indices[1]);
            Tests.AssertEqual(-1, indices[2]);
            Tests.AssertEqual(34, indices[3]);
            Tests.AssertEqual("right,left", string.Join(",", requested));
            Tests.AssertTrue(ReferenceEquals(holsters, item.ItemHolsters));
            Tests.AssertTrue(item.ItemHolsters[1] == null, "The shared item definition must not be rewritten.");
        }

        [GameTest("HolsterFallbackHandlesMissingAndOversizedArrays", "equipment", RequiresCampaign = false)]
        public static void HolsterFallbackHandlesMissingAndOversizedArrays()
        {
            foreach (var holsters in new string[][] { null, new string[4], new[] { "", "", "", "", "unused" } })
            {
                var item = Weapon(holsters);
                Tests.AssertTrue(ItemHolsterSafety.TryGetFallbackIndices(item,
                    _ => throw new InvalidOperationException("Missing entries and overflow must not reach native lookup."), out var indices));
                for (int i = 0; i < 4; i++) Tests.AssertEqual(-1, indices[i]);
                Tests.AssertTrue(ReferenceEquals(holsters, item.ItemHolsters));
            }
        }

        [GameTest("ValidHolstersKeepOriginalEnginePath", "equipment", RequiresCampaign = false)]
        public static void ValidHolstersKeepOriginalEnginePath()
        {
            foreach (var holsters in new[] { new string[0], new[] { "" }, new[] { "a", "b", "", "d" } })
                Tests.AssertFalse(ItemHolsterSafety.TryGetFallbackIndices(Weapon(holsters),
                    _ => throw new InvalidOperationException("A valid weapon must use the original engine method."), out _));
        }

        [GameTest("MalformedNewGearDoesNotEraseExistingGearOrQueuedProgress", "save-contracts", RequiresCampaign = false)]
        public static void MalformedNewGearDoesNotEraseExistingGearOrQueuedProgress()
        {
            var equipment = Equipment();
            var saved = Weapon(new[] { "", null });
            equipment.Base[EquipmentIndex.Weapon0] = new EquipmentElement(saved);
            equipment.ItemsStaging = new List<string> { "0|pending_holster_fixture" };
            equipment.ItemStagingProgress = 7.5f;
            Tests.AssertEqual(0, equipment.ScrubInvalidItems(_ => new WItem(saved)), "Do not delete malformed saved gear or its replacement plan.");
            var blocked = (bool)AccessTools.Method(typeof(MEquipment), "SetBase").Invoke(equipment,
                new object[] { EquipmentIndex.Weapon0, new WItem(Weapon(null)) });
            Tests.AssertFalse(blocked);
            Tests.AssertTrue(ReferenceEquals(saved, equipment.Base[EquipmentIndex.Weapon0].Item));
            Tests.AssertEqual("0|pending_holster_fixture", equipment.ItemsStaging[0]);
            Tests.AssertEqual(7.5f, equipment.ItemStagingProgress);
        }
    }
}
