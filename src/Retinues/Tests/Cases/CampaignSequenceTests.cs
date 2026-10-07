using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Staging;
using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor;
using Retinues.Editor.MVC.Pages.Equipment.Services;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    public static class CampaignSequenceTests
    {
        [GameTest("CloneEditRestoreRemoveRepeatedly", "sequences",
            "Thirty clone/edit/three-load/remove cycles preserve source troops, skills and equipment sets")]
        public static void CloneEditRestoreRemoveRepeatedly(GameTestContext ctx)
        {
            using var instant = TestConfig.Set(Configuration.EquippingTakesTime, false);
            var sourceObject = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Tests.AssertNotNull(sourceObject, "This scenario requires the standard looter template.");
            var source = WCharacter.Get(sourceObject);
            string sourceName = source.Name;
            int sourceSkill = source.Skills[DefaultSkills.OneHanded];
            string sourceGear = string.Join(";", source.Equipments.Select(e => e.Code));
            ctx.Defer(() =>
            {
                Tests.AssertEqual(sourceName, source.Name, "Clone edits cannot rename their source.");
                Tests.AssertEqual(sourceSkill, source.Skills[DefaultSkills.OneHanded], "Clone skill storage is independent.");
                Tests.AssertEqual(sourceGear, string.Join(";", source.Equipments.Select(e => e.Code)), "Clone equipment storage is independent.");
            });
            var body = WItem.GetEquipmentsForSlot(EquipmentIndex.Body).FirstOrDefault(i => i?.IsValidEquipment == true);
            Tests.AssertNotNull(body, "A valid body item is required.");
            int activeBefore = ActiveCount();
            for (int cycle = 0; cycle < 30; cycle++)
            {
                ctx.Case(cycle, () =>
                {
                    using var sandbox = new TestSandbox();
                    var troop = sandbox.Track(CharacterCloner.Clone(source));
                    Tests.AssertNotNull(troop);
                    string name = "Garde <&> " + cycle;
                    int skill = ctx.Random.Next(1, 301);
                    troop.Name = name;
                    troop.Skills[DefaultSkills.OneHanded] = skill;
                    var battle = MEquipment.Create(troop);
                    battle.Set(EquipmentIndex.Body, body);
                    battle.SiegeBattleSet = cycle % 2 == 0;
                    troop.EquipmentRoster.Equipments = new List<MEquipment> { battle, MEquipment.Create(troop, civilian: true) };
                    for (int load = 0; load < 3; load++)
                    {
                        string saved = troop.Serialize();
                        Tests.AssertTrue(!string.IsNullOrWhiteSpace(saved));
                        troop.Name = "mutated";
                        troop.Skills[DefaultSkills.OneHanded] = 0;
                        troop.EquipmentRoster.Reset();
                        troop.Deserialize(saved);
                        troop.InvalidateEquipmentRosterCache();
                        Tests.AssertEqual(name, troop.Name);
                        Tests.AssertEqual(skill, troop.Skills[DefaultSkills.OneHanded]);
                        Tests.AssertEqual(source.StringId, troop.SourceStringId);
                        Tests.AssertEqual(2, troop.Equipments.Count);
                        Tests.AssertFalse(troop.Equipments[0].IsCivilian);
                        Tests.AssertTrue(troop.Equipments[1].IsCivilian);
                        Tests.AssertEqual(cycle % 2 == 0, troop.Equipments[0].SiegeBattleSet);
                        Tests.AssertEqual(body.StringId, troop.Equipments[0].GetBase(EquipmentIndex.Body)?.StringId);
                        Tests.AssertNotNull(troop.Base.FirstBattleEquipment, "The engine must still see battle equipment.");
                    }
                });
                Tests.AssertEqual(activeBefore, ActiveCount(), "Each cycle returns its allocation before the next cycle.");
            }
        }

        [GameTest("EquipmentEconomySharingAndModeMatrix", "sequences",
            "Sharing, stock, purchases, preview and editor mode combinations conserve required item counts")]
        public static void EquipmentEconomySharingAndModeMatrix(GameTestContext ctx)
        {
            using var instant = TestConfig.Set(Configuration.EquippingTakesTime, false);
            using var sandbox = new TestSandbox();
            var troop = sandbox.NewStub();
            Tests.AssertNotNull(troop);
            var item = WItem.GetEquipmentsForSlot(EquipmentIndex.Weapon0).FirstOrDefault(i => i?.IsValidEquipment == true && i.Value > 0);
            Tests.AssertNotNull(item, "A priced weapon is required.");
            int savedStock = item.Stock;
            ctx.Defer(() => { item.Stock = savedStock; Tests.AssertEqual(savedStock, item.Stock); });
            var target = MEquipment.Create(troop);
            var other = MEquipment.Create(troop);
            troop.EquipmentRoster.Equipments = new List<MEquipment> { target, other };
            for (int otherCount = 0; otherCount <= 4; otherCount++)
            {
                for (int slot = 0; slot < 4; slot++)
                    other.Set((EquipmentIndex)slot, slot < otherCount ? item : null);
                for (int before = 0; before <= 4; before++)
                for (int after = 0; after <= 4; after++)
                for (int stock = 0; stock <= 2; stock++)
                foreach (bool costs in new[] { true, false })
                foreach (bool preview in new[] { true, false })
                foreach (var mode in new[] { EditorMode.Player, EditorMode.Universal })
                {
                    using var money = TestConfig.Set(Configuration.EquipmentCostsMoney, costs);
                    item.Stock = stock;
                    int additional = Math.Max(0, Math.Max(otherCount, after) - Math.Max(otherCount, before));
                    bool enabled = costs && !preview && mode == EditorMode.Player;
                    int stockUse = enabled ? Math.Min(stock, additional) : 0;
                    int purchase = enabled ? additional - stockUse : 0;
                    var plan = new EquipPlan();
                    EquipEconomy.ComputeBatchEconomy(new EquipContext(mode, preview, troop, target), target,
                        Enumerable.Repeat(item, before).ToArray(), Enumerable.Repeat(item, after).ToArray(), plan);
                    string scenario = $"other={otherCount}, before={before}, after={after}, stock={stock}, costs={costs}, preview={preview}, mode={mode}";
                    Tests.AssertEqual(stockUse, plan.StockUseTotal, scenario);
                    Tests.AssertEqual(purchase, plan.PurchaseTotal, scenario);
                    Tests.AssertEqual(EquipEconomy.ComputeEquipCost(item) * purchase, plan.TotalCost, scenario);
                    Tests.AssertEqual(stock, item.Stock, "Planning is read-only: " + scenario);
                }
            }
        }

        [GameTest("StagedQueueSurvivesReloadAndDifferentTickSizes", "sequences",
            "A FIFO queue completes identically with one large tick or many small ticks after a restore")]
        public static void StagedQueueSurvivesReloadAndDifferentTickSizes(GameTestContext ctx)
        {
            using var config = TestConfig.Set(Configuration.EquippingTakesTime, true);
            using var sandbox = new TestSandbox();
            var oldMode = EditorState.Instance.Mode;
            ctx.Defer(() =>
            {
                Reflection.SetPropertyValue(EditorState.Instance, "Mode", oldMode);
                Tests.AssertEqual(oldMode, EditorState.Instance.Mode);
            });
            Reflection.SetPropertyValue(EditorState.Instance, "Mode", EditorMode.Universal);
            var weapons = WItem.GetEquipmentsForSlot(EquipmentIndex.Weapon0)
                .Where(i => i?.IsValidEquipment == true && i.Value > 100).Take(3).ToList();
            if (weapons.Count < 3) Tests.Skip("Requires three valid priced weapons.");
            var troop = sandbox.NewStub();
            Tests.AssertNotNull(troop);
            var first = MEquipment.Create(troop);
            var second = MEquipment.Create(troop);
            troop.EquipmentRoster.Equipments = new List<MEquipment> { first, second };
            var queue = weapons.Select((item, slot) => slot + "|" + item.StringId).ToList();
            first.ItemsStaging = queue;
            second.ItemsStaging = queue;
            float totalHours = weapons.Sum(w => w.Value / 1000f * 24f);
            first.ItemStagingProgress = second.ItemStagingProgress = totalHours / 100f;
            string payload = troop.Serialize();
            troop.EquipmentRoster.Reset();
            troop.Deserialize(payload);
            troop.InvalidateEquipmentRosterCache();
            first = troop.Equipments[0];
            second = troop.Equipments[1];
            Tests.AssertTrue(first.ItemsStaging.SequenceEqual(queue));
            Tests.AssertEqual(totalHours / 100f, first.ItemStagingProgress);
            var largeOrder = new List<string>();
            var smallOrder = new List<string>();
            StagedItemProgressBehavior.AdvanceEquipment(first, totalHours, 1f, w => largeOrder.Add(w.StringId));
            for (int step = 0; step < 100; step++)
                StagedItemProgressBehavior.AdvanceEquipment(second, totalHours / 100f, 1f, w => smallOrder.Add(w.StringId));
            Tests.AssertTrue(largeOrder.SequenceEqual(weapons.Select(w => w.StringId)), "Large ticks complete in FIFO order.");
            Tests.AssertTrue(smallOrder.SequenceEqual(largeOrder), "Time slicing cannot drop or duplicate work.");
            Tests.AssertFalse(first.HasAnyStagedItems());
            Tests.AssertFalse(second.HasAnyStagedItems());
            Tests.AssertEqual(0f, first.ItemStagingProgress);
            Tests.AssertEqual(0f, second.ItemStagingProgress);
            for (int slot = 0; slot < weapons.Count; slot++)
            {
                Tests.AssertEqual(weapons[slot].StringId, first.GetBase((EquipmentIndex)slot)?.StringId);
                Tests.AssertEqual(weapons[slot].StringId, second.GetBase((EquipmentIndex)slot)?.StringId);
            }
        }

        private static int ActiveCount() => WCharacter.All.Count(c => c.IsCustom && c.IsActiveStub);
    }
}
