using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

// Isolated host only: installs the real initializer patch while stubbing the engine's
// campaign/native initialization. Then feeds its roster to the production unlock handler.
internal static class DiscardUnlockProbe
{
    private static bool CheatEnabled;
    private static bool SkipInitialize() => false;
    private static bool CheatMode(ref bool __result) { __result = CheatEnabled; return false; }

    internal static void Run(Assembly module, string branch)
    {
        bool v2 = branch == "v2";
        string ns = v2 ? "Retinues.Behaviors.Unlocks" : "Retinues.Features.Unlocks";
        var patch = module.GetType(ns + ".Patches.CheatInventoryDiscardPatch", true);
        var initialize = (MethodBase)patch.GetMethod("TargetMethod", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        var harmony = new Harmony("retinues.headless.discard-unlocks");
        var currentField = AccessTools.Field(typeof(Game), "_current");
        var previousGame = currentField.GetValue(null);
        var scopes = new List<IDisposable>();
        try
        {
            harmony.CreateClassProcessor(patch).Patch();
            // BL12 already suppresses the game's discard event for cheat inventories.
            // Its patch binding and shared filter are still verified by this host/suite.
            if (typeof(InventoryLogic).Assembly.GetType("TaleWorlds.CampaignSystem.Inventory.InventoryManager") != null)
                return;

            harmony.Patch(initialize, prefix: new HarmonyMethod(typeof(DiscardUnlockProbe), nameof(SkipInitialize)) { priority = Priority.Last });
            harmony.Patch(AccessTools.PropertyGetter(typeof(Game), "CheatMode"), prefix: new HarmonyMethod(typeof(DiscardUnlockProbe), nameof(CheatMode)));
            currentField.SetValue(null, FormatterServices.GetUninitializedObject(typeof(Game)));
            MBObjectManager.Init();
            MBObjectManager.Instance.RegisterType<ItemObject>("Item", "Items", 1);
            var bow = new ItemObject("discard_probe_bow") { Type = ItemObject.ItemTypeEnum.Bow };
            bow.AddWeapon(new WeaponComponentData(bow, WeaponClass.Bow, WeaponFlags.RangedWeapon), null);
            AccessTools.PropertySetter(typeof(ItemObject), "ItemHolsters").Invoke(bow, new object[] { new string[0] });
            var armor = new ItemObject("discard_probe_armor") { Type = ItemObject.ItemTypeEnum.BodyArmor };
            MBObjectManager.Instance.RegisterObject(bow);
            MBObjectManager.Instance.RegisterObject(armor);

            var config = module.GetType(v2 ? "Retinues.Settings.Configuration" : "Retinues.Configuration.Config", true);
            if (!v2) config.GetMethod("DiscoverOptions", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            else module.GetType("Retinues.Behaviors.Doctrines.DoctrinesRegistry").GetMethod("EnsureRegistered").Invoke(null, null);
            SetOption(module, config, scopes, v2 ? "EquipmentNeedsUnlocking" : "AllEquipmentUnlocked", v2);
            SetOption(module, config, scopes, v2 ? "UnlockItemsThroughDiscards" : "UnlockItemsFromDiscards", false);
            SetOption(module, config, scopes, v2 ? "RequiredDiscardsToUnlock" : "RequiredDiscardsPerItem", 100);
            if (!v2) SetOption(module, config, scopes, "RequiredKillsPerItem", 100);

            var roster = new ItemRoster();
            roster.AddToCounts(bow, 10);
            roster.AddToCounts(armor, 10);
            CheatEnabled = true;
            Open(initialize, roster, "Default", false);
            // Enabling unlocks mid-screen and disabling cheats must not lose the snapshot.
            SetOption(module, config, scopes, v2 ? "UnlockItemsThroughDiscards" : "UnlockItemsFromDiscards", true);
            CheatEnabled = false;
            roster.AddToCounts(bow, 1);
            var behaviorType = module.GetType(ns + (v2 ? ".UnlocksByDiscardsBehavior" : ".UnlocksBehavior"), true);
            var behavior = Activator.CreateInstance(behaviorType);
            behaviorType.GetMethod("OnItemsDiscardedByPlayer", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(behavior, new object[] { roster });
            Require(Progress(module, v2, bow) == (v2 ? 10 : 1), "Only one real bow discard should gain progress.");
            Require(Progress(module, v2, armor) == 0, "Untouched catalogue armor gained progress.");
            Require(roster.GetItemNumber(bow) == 11 && roster.GetItemNumber(armor) == 10, "Event roster was modified.");

            var filter = module.GetType(ns + ".CheatDiscardFilter", true).GetMethod("ForUnlocks", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var mode in new[] { "Loot", "Trade", "Stash", "Warehouse" })
            {
                var normal = new ItemRoster();
                normal.AddToCounts(armor, 10);
                CheatEnabled = true;
                Open(initialize, normal, mode, mode == "Trade");
                Require(ReferenceEquals(normal, filter.Invoke(null, new object[] { normal })), mode + " incorrectly treated as a cheat catalogue.");
            }
            var ordinary = new ItemRoster();
            CheatEnabled = false;
            Open(initialize, ordinary, "Default", false);
            CheatEnabled = true; // Turning cheats on after opening cannot change a normal discard.
            ordinary.AddToCounts(armor, 1);
            Require(ReferenceEquals(ordinary, filter.Invoke(null, new object[] { ordinary })), "Closing-time cheat mode changed a normal discard.");
        }
        finally
        {
            for (int i = scopes.Count - 1; i >= 0; i--) scopes[i].Dispose();
            currentField.SetValue(null, previousGame);
            harmony.UnpatchAll(harmony.Id);
        }
    }

    private static void Open(MethodBase initialize, ItemRoster roster, string mode, bool trading)
    {
        var args = initialize.GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
        var parameters = initialize.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].Name == "leftItemRoster") args[i] = roster;
            if (parameters[i].Name == "isTrading") args[i] = trading;
            if (parameters[i].Name == "inventoryMode") args[i] = Enum.Parse(parameters[i].ParameterType, mode);
        }
        initialize.Invoke(FormatterServices.GetUninitializedObject(typeof(InventoryLogic)), args);
    }

    private static void SetOption(Assembly module, Type config, List<IDisposable> scopes, string name, object value)
    {
        var option = config.GetField(name).GetValue(null);
        scopes.Add((IDisposable)module.GetType("Retinues.Tests.TestConfig").GetMethod("Set")
            .MakeGenericMethod(value.GetType()).Invoke(null, new[] { option, value }));
    }

    private static int Progress(Assembly module, bool v2, ItemObject item)
    {
        if (!v2) return (int)module.GetType("Retinues.Features.Unlocks.UnlocksBehavior").GetMethod("GetProgress").Invoke(null, new object[] { item.StringId });
        var type = module.GetType("Retinues.Domain.Equipments.Wrappers.WItem", true);
        var wrapper = type.GetMethod("Get", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, new[] { typeof(ItemObject) }, null).Invoke(null, new object[] { item });
        return (int)type.GetProperty("UnlockProgress").GetValue(wrapper);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
