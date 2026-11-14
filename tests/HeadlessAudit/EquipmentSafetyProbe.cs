using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

// Runs only in the headless host. Never replace the object manager in a live campaign test.
internal static class EquipmentSafetyProbe
{
    internal static void Run(Assembly module)
    {
        typeof(MBObjectManager).GetMethod("Init").Invoke(null, null);
        var manager = MBObjectManager.Instance;
        manager.RegisterType<ItemObject>("Item", "Items", 1);
        var wrapperType = module.GetType("Retinues.Domain.Equipments.Wrappers.WItem", true);
        var get = wrapperType.GetMethod("Get", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
            null, new[] { typeof(ItemObject) }, null);
        var bad = Weapon("headless_holster_swap", null);
        manager.RegisterObject(bad);
        var cached = get.Invoke(null, new object[] { bad });
        wrapperType.GetProperty("Stock").SetValue(cached, 9);
        wrapperType.GetProperty("UnlockProgress").SetValue(cached, 437);
        string savedAttributes = (string)wrapperType.GetMethod("Serialize").Invoke(cached, null);

        // A pre-existing queue must wait without losing earned progress or its current item.
        var eq = module.GetType("Retinues.Tests.Cases.EquipmentHolsterTests", true)
            .GetMethod("Equipment", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        var eqType = eq.GetType();
        eqType.GetProperty("ItemsStaging").SetValue(eq, new List<string> { "0|headless_holster_swap" });
        eqType.GetProperty("ItemStagingProgress").SetValue(eq, 7.5f);
        var hours = eqType.GetMethod("GetNextStagedHours", BindingFlags.NonPublic | BindingFlags.Instance);
        var apply = eqType.GetMethod("TryApplyNextStagedItem", BindingFlags.NonPublic | BindingFlags.Instance);
        Require(float.IsPositiveInfinity((float)hours.Invoke(eq, new object[] { 1f })), "Malformed queued item must wait.");
        Require(!(bool)apply.Invoke(eq, new object[] { EquipmentIndex.None, null, false }), "Malformed queued item must not be applied.");
        Require((float)eqType.GetProperty("ItemStagingProgress").GetValue(eq) == 7.5f, "Queue progress was erased.");
        Require(((List<string>)eqType.GetProperty("ItemsStaging").GetValue(eq)).Count == 1, "Queue was erased.");

        // Keep a previously returned wrapper alive across replacement, just like an open editor.
        manager.UnregisterObject(bad);
        var good = Weapon(bad.StringId, new[] { "" });
        manager.RegisterObject(good);
        var replacement = get.Invoke(null, new object[] { good });
        Require(ReferenceEquals(cached, replacement), "Replacing an item must retain the wrapper's dirty persistence attributes.");
        Require(ReferenceEquals(wrapperType.GetProperty("Base").GetValue(cached), good), "Cached wrapper still uses obsolete item.");
        Require(ReferenceEquals(wrapperType.GetProperty("Base").GetValue(get.Invoke(null, new object[] { bad })), good), "Old references must not poison the cache again.");
        Require(savedAttributes == (string)wrapperType.GetMethod("Serialize").Invoke(cached, null), "Stock/unlock persistence changed during replacement.");
        Require((bool)wrapperType.GetProperty("IsValidEquipment").GetValue(cached), "Corrected definition must become usable.");
        Require((bool)apply.Invoke(eq, new object[] { EquipmentIndex.None, null, false }), "Pending item must resume after its definition is corrected.");
        var baseEquipment = (Equipment)eqType.GetProperty("Base").GetValue(eq);
        Require(ReferenceEquals(baseEquipment[EquipmentIndex.Weapon0].Item, good), "Equip reused obsolete definition.");
        Require(((List<string>)eqType.GetProperty("ItemsStaging").GetValue(eq)).Count == 0, "Completed queue entry remains.");

        // Exercise the installed Harmony prefix on the actual engine method, not a mirror.
        var patcher = module.GetType("Retinues.Behaviors.Agents.Patches.ItemHolsterSafetyPatch", true);
        var harmonyType = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony", true);
        var harmony = Activator.CreateInstance(harmonyType, new object[] { "retinues.headless.holsters" });
        var processor = harmonyType.GetMethod("CreateClassProcessor").Invoke(harmony, new object[] { patcher });
        try
        {
            processor.GetType().GetMethod("Patch").Invoke(processor, null);
            var method = Assembly.Load("TaleWorlds.MountAndBlade")
                .GetType("TaleWorlds.MountAndBlade.ItemCollectionElementMissionExtensions", true).GetMethod("GetItemHolsterIndices");
            foreach (var holsters in new string[][] { null, new string[4], new[] { "", null, "" }, new string[0], new[] { "" } })
            {
                var indices = (StackArray.StackArray4Int)method.Invoke(null, new object[] { Weapon("headless_spawn", holsters) });
                for (int i = 0; i < 4; i++) Require(indices[i] == -1, "Incorrect no-holster fallback.");
            }
        }
        finally { harmonyType.GetMethod("UnpatchAll").Invoke(harmony, new object[] { "retinues.headless.holsters" }); }
    }

    private static ItemObject Weapon(string id, string[] holsters)
    {
        var item = new ItemObject(id) { Type = ItemObject.ItemTypeEnum.OneHandedWeapon };
        item.AddWeapon(new WeaponComponentData(item, WeaponClass.OneHandedSword, WeaponFlags.MeleeWeapon), null);
        typeof(ItemObject).GetProperty("ItemHolsters").GetSetMethod(true).Invoke(item, new object[] { holsters });
        return item;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
