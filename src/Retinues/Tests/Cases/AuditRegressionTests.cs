using System;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using Retinues.Behaviors.Experience;
using Retinues.Behaviors.Retinues;
using Retinues.Behaviors.Retinues.Patches;
using Retinues.Behaviors.Staging;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Editor;
using Retinues.Editor.MVC.Pages.Equipment.Services;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Tests.Cases
{
    public static class AuditRegressionTests
    {
        [GameTest("BattleRewardWrapperReturnsValues", "audit", RequiresCampaign = false)]
        public static void BattleRewardWrapperReturnsValues()
        {
            // Managed engine fixtures: no campaign, real party, battle, or save is modified.
            T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
            void Field(object target, string name, object value) =>
                AccessTools.Field(target.GetType(), name).SetValue(target, value);
            void Property(object target, string name, object value) =>
                Field(target, $"<{name}>k__BackingField", value);

            var battle = Empty<MapEvent>();
            var party = Empty<PartyBase>();
            Property(party, "MemberRoster", TroopRoster.CreateDummyTroopRoster());
            var entry = Empty<MapEventParty>();
            Property(entry, "Party", party);
#if BL14
            Property(entry, "GainedRenownExplained", new ExplainedNumber(7f));
            Property(entry, "GainedInfluenceExplained", new ExplainedNumber(3f));
            Property(entry, "GainedMoraleExplained", new ExplainedNumber(11f));
#else
            Property(entry, "GainedRenown", 7f);
            Property(entry, "GainedInfluence", 3f);
            Property(entry, "MoraleChange", 11f);
#endif
            Property(entry, "PlunderedGold", 200);
            Property(entry, "GoldLost", 75);
            Field(entry, "_contributionToBattle", 100);

            var defender = Empty<MapEventSide>();
            var attacker = Empty<MapEventSide>();
            Field(defender, "_battleParties", new MBList<MapEventParty> { entry });
            Field(attacker, "_battleParties", new MBList<MapEventParty>());
            Property(defender, "MissionSide", BattleSideEnum.Defender);
            Field(party, "_mapEventSide", defender);
            Field(battle, "_sides", new[] { defender, attacker });

            // Validate the legacy engine fixture before testing the wrapper's version dispatch.
            var legacyReader = AccessTools.Method(typeof(MapEvent), "GetBattleRewards");
            if (legacyReader != null)
            {
                object[] rewards = { party, 0f, 0f, 0f, 0f, 0f };
                legacyReader.Invoke(battle, rewards);
                Tests.AssertEqual(7f, (float)rewards[1], "The engine fixture supplies a nonzero reward.");
            }

            var wrapped = new MMapEvent(battle);
            wrapped.ComputeRewards(party);
            Tests.AssertEqual(7, wrapped.RenownReward, "The wrapper returns actual renown.");
            Tests.AssertEqual(3, wrapped.InfluenceReward, "The wrapper returns actual influence.");
            Tests.AssertEqual(11, wrapped.MoraleReward, "The wrapper returns actual morale.");
            Tests.AssertEqual(125, wrapped.GoldReward, "Gold is plundered minus lost.");
            Property(entry, "GoldLost", 220);
            Tests.AssertEqual(125, wrapped.GoldReward, "The reward snapshot stays cached.");
            var next = new MMapEvent(battle);
            next.ComputeRewards(party);
            Tests.AssertEqual(-20, next.GoldReward, "Net gold losses retain their sign.");
        }

        [GameTest("SharedXpUsesContributorCost", "audit", RequiresCampaign = false)]
        public static void SharedXpUsesContributorCost()
        {
            double highFirst = 0, lowFirst = 0;
            int forward = SkillPointProgress.Add(ref highFirst, 33000, 34000);
            forward += SkillPointProgress.Add(ref highFirst, 1, 6000);
            int reverse = SkillPointProgress.Add(ref lowFirst, 1, 6000);
            reverse += SkillPointProgress.Add(ref lowFirst, 33000, 34000);
            Tests.AssertEqual(0, forward, "A cheap troop cannot revalue expensive troop XP.");
            Tests.AssertEqual(forward, reverse, "Award order does not change whole points.");
            Tests.AssertTrue(Math.Abs(highFirst - lowFirst) < 1e-12, "Remainders agree.");

            forward += SkillPointProgress.Add(ref highFirst, 1000, 34000);
            forward += SkillPointProgress.Add(ref highFirst, 5999, 6000);
            Tests.AssertEqual(2, forward, "Both troops' completed points are retained.");
            Tests.AssertTrue(highFirst < 1e-10, "Completed progress leaves no material remainder.");

            double split = 0;
            int points = 0;
            for (int i = 0; i < 100; i++)
                points += SkillPointProgress.Add(ref split, 1, 100);
            Tests.AssertEqual(1, points, "Splitting XP into small grants still completes a point.");
        }

        [GameTest("LegacySharedXpMigratesOnce", "audit", RequiresCampaign = false)]
        public static void LegacySharedXpMigratesOnce()
        {
            double progress = 0;
            int legacy = 33000;
            Tests.AssertEqual(0, SkillPointProgress.MigrateLegacy(ref progress, ref legacy, 0),
                "No eligible cost defers migration.");
            Tests.AssertEqual(33000, legacy, "Deferred XP is preserved.");
            Tests.AssertEqual(5, SkillPointProgress.MigrateLegacy(ref progress, ref legacy, 6000),
                "Old XP keeps its best attainable value.");
            Tests.AssertEqual(0, legacy, "Converted XP is cleared.");
            Tests.AssertEqual(0.5d, progress, "The fractional remainder is retained.");
            Tests.AssertEqual(0, SkillPointProgress.MigrateLegacy(ref progress, ref legacy, 6000),
                "Repeating migration cannot award points twice.");
            Tests.AssertEqual(0.5d, progress, "Repeated migration keeps the remainder.");
        }

        [GameTest("CraftedSelectionKeepsOwnedIds", "audit", RequiresCampaign = false)]
        public static void CraftedSelectionKeepsOwnedIds()
        {
            var copies = new[] { "sold", "stock", "equipped", "other-design", "ordinary" };
            string Design(string id) => id == "ordinary" ? null
                : id == "other-design" ? "second" : "same";
            var selected = CraftedItemSelection.Select(copies, Design,
                id => id == "stock" || id == "equipped");
            Tests.AssertTrue(selected.SequenceEqual(new[] { "stock", "equipped", "other-design", "ordinary" }),
                "All owned IDs survive, with one unowned representative per other design.");
            selected = CraftedItemSelection.Select(copies, Design, _ => false);
            Tests.AssertTrue(selected.SequenceEqual(new[] { "sold", "other-design", "ordinary" }),
                "Unused duplicate designs collapse to one row.");
            selected = CraftedItemSelection.Select(copies, Design, id => id == "equipped");
            Tests.AssertEqual("equipped", selected[0], "Selection follows changing troop ownership.");
        }

        [GameTest("RetinueUpgradeBatchesAreClamped", "audit", RequiresCampaign = false)]
        public static void RetinueUpgradeBatchesAreClamped()
        {
            foreach (int request in new[] { 1, 5, 99 })
            {
                var command = new PartyScreenLogic.PartyCommand();
                command.FillForUpgradeTroop(PartyScreenLogic.PartyRosterSide.Right,
                    PartyScreenLogic.TroopType.Member, null, request, 2, 7);
                Tests.AssertTrue(RetinueDynamicUpgradePatch.ClampUpgradeCommand(command, 3),
                    "Some capacity permits an upgrade.");
                Tests.AssertEqual(Math.Min(request, 3), command.TotalNumber, "Batch fits remaining capacity.");
                Tests.AssertEqual(2, command.UpgradeTarget, "Clamping preserves the upgrade target.");
                Tests.AssertEqual(7, command.Index, "Clamping preserves insertion order.");
                Tests.AssertFalse(RetinueDynamicUpgradePatch.ClampUpgradeCommand(command, 0),
                    "A full party rejects the command before costs are spent.");
            }
        }

        [GameTest("EquipmentScrubPreservesValidPlans", "audit")]
        public static void EquipmentScrubPreservesValidPlans(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();
            using var config = TestConfig.Set(Configuration.EquippingTakesTime, true);
            var originalMode = EditorState.Instance.Mode;
            try
            {
                var valid = WItem.GetEquipmentsForSlot(EquipmentIndex.Weapon0).First();
                var invalid = WItem.All.FirstOrDefault(i => i.IsUnsafeForTroopEquipment);
                if (invalid == null) Tests.Skip("Requires identified mission/siege ammunition.");
                var owner = sandbox.NewStub();
                var set = MEquipment.Create(owner);
                owner.EquipmentRoster.Equipments = [set];
                foreach (var mode in new[] { EditorMode.Player, EditorMode.Universal })
                {
                    Reflection.SetPropertyValue(EditorState.Instance, "Mode", mode);
                    set.Base[EquipmentIndex.Weapon0] = new EquipmentElement(invalid.Base);
                    string replacement = $"0|{valid.StringId}";
                    set.ItemsStaging = [replacement, $"1|{invalid.StringId}", "malformed", "2|audit_missing_item"];
                    set.ItemStagingProgress = 3f;
                    Tests.AssertEqual(3, set.ScrubInvalidItems(), "Unsafe real/queued gear and malformed work are removed; unresolved IDs survive.");
                    Tests.AssertTrue(set.Base[EquipmentIndex.Weapon0].IsEmpty, "The real invalid item is cleared immediately.");
                    Tests.AssertTrue(set.ItemsStaging.SequenceEqual(new[] { replacement, "2|audit_missing_item" }), "The valid replacement and unresolved work survive in order.");
                    Tests.AssertEqual(3f, set.ItemStagingProgress, "Work on the valid head survives.");
                    Tests.AssertTrue(set.TryApplyNextStagedItem(out _, out _, out _), "The replacement applies.");
                    Tests.AssertEqual(valid.StringId, set.GetBase(EquipmentIndex.Weapon0)?.StringId, "Only valid gear is equipped.");

                    set.ItemsStaging = [$"0|{invalid.StringId}"];
                    Tests.AssertFalse(set.TryApplyNextStagedItem(out _, out _, out _), "The worker independently rejects invalid gear.");
                    Tests.AssertFalse(set.HasAnyStagedItems(), "Rejected work is removed.");
                    Tests.AssertEqual(valid.StringId, set.GetBase(EquipmentIndex.Weapon0)?.StringId,
                        "Rejecting a queued invalid item preserves valid real equipment in its slot.");
                }
            }
            finally { Reflection.SetPropertyValue(EditorState.Instance, "Mode", originalMode); }
        }

        [GameTest("StagedGearProgressesAfterLoadInUniversalMode", "audit")]
        public static void StagedGearProgressesAfterLoadInUniversalMode(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();
            using var config = TestConfig.Set(Configuration.EquippingTakesTime, true);
            var originalMode = EditorState.Instance.Mode;
            try
            {
                var valid = WItem.GetEquipmentsForSlot(EquipmentIndex.Weapon0).First(i => i.Value > 100);
                var owner = sandbox.NewStub();
                var set = MEquipment.Create(owner);
                owner.EquipmentRoster.Equipments = [set];
                Reflection.SetPropertyValue(EditorState.Instance, "Mode", EditorMode.Player);
                set.Set(EquipmentIndex.Weapon0, valid);
                float required = set.GetNextStagedHours(1f);
                set.ItemStagingProgress = required / 2f;
                var saved = owner.Serialize();
                owner.EquipmentRoster.Equipments = [MEquipment.Create(owner)];
                owner.Deserialize(saved);
                owner.InvalidateEquipmentRosterCache();
                set = owner.Equipments[0];
                Reflection.SetPropertyValue(EditorState.Instance, "Mode", EditorMode.Universal);
                Tests.AssertTrue(StagedItemProgressBehavior.AdvanceEquipment(set, required / 2f, 1f),
                    "Saved work completes while the editor is in Universal mode.");
                Tests.AssertEqual(valid.StringId, set.GetBase(EquipmentIndex.Weapon0)?.StringId, "The queued item became real gear.");
                Tests.AssertFalse(set.HasAnyStagedItems(), "Completed work is removed from the queue.");
            }
            finally { Reflection.SetPropertyValue(EditorState.Instance, "Mode", originalMode); }
        }

        [GameTest("RetinueCapacityUsesPendingRoster", "audit")]
        public static void RetinueCapacityUsesPendingRoster(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var config = TestConfig.Set(Configuration.EnableRetinues, true);
            using var sandbox = new TestSandbox();
            var culture = WCulture.All.First(c => (c.RootElite ?? c.RootBasic)?.Base != null);
            var retinue = sandbox.Track(RetinuesBehavior.Instance.CreateRetinue(culture, "Audit retinue", false));
            Tests.AssertNotNull(retinue, "Created a temporary retinue.");
            var source = (culture.RootBasic ?? culture.RootElite).Base;
            var pending = TroopRoster.CreateDummyTroopRoster();
            pending.AddToCounts(retinue.Base, 8, woundedCount: 2);
            Tests.AssertEqual(2, RetinueDynamicUpgradePatch.GetRemainingCapacity(10, pending, source),
                "Wounded retinues occupy capacity too.");
            pending.AddToCounts(retinue.Base, 2);
            Tests.AssertEqual(0, RetinueDynamicUpgradePatch.GetRemainingCapacity(10, pending, source),
                "An earlier upgrade in the same screen consumes capacity immediately.");
            pending.AddToCounts(retinue.Base, -4);
            Tests.AssertEqual(4, RetinueDynamicUpgradePatch.GetRemainingCapacity(10, pending, source),
                "Pending transfers out restore capacity immediately.");
            Tests.AssertEqual(int.MaxValue, RetinueDynamicUpgradePatch.GetRemainingCapacity(0, pending, retinue.Base),
                "Retinue-to-retinue upgrades do not consume additional capacity.");
        }
    }
}
