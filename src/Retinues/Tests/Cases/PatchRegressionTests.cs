using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Retinues.Behaviors.Doctrines;
using Retinues.Behaviors.Tournaments.Patches;
using Retinues.Behaviors.Unlocks;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Regression tests for the game-1.4/1.5 compatibility patch batch: tournament bracket
    /// integrity, kill-unlock multipliers, battle-reward sources, clone FillFrom, crafted-item
    /// cache invalidation and the doctrine reset command.
    /// </summary>
    public static class PatchRegressionTests
    {
        /// <summary>
        /// The tournament participant list must never shrink: stripping custom troops has to
        /// refill the bracket with vanilla troops (a short list crashes the bracket builder).
        /// </summary>
        [GameTest(
            "TournamentStripRefillsBracket",
            "regression",
            "Stripping customs from a tournament roster refills it to the original count with vanilla troops"
        )]
        public static void TournamentStripRefillsBracket(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var settlement = Settlement.All.FirstOrDefault(s =>
                s?.IsTown == true && s.Culture?.BasicTroop != null
            );
            Tests.AssertNotNull(settlement, "Found a town with a culture troop tree.");

            var custom = sandbox.NewStub();
            Tests.AssertNotNull(custom?.Base, "Allocated a custom stub for the bracket.");

            var result = new MBList<CharacterObject> { settlement.Culture.BasicTroop };
            var removed = new List<CharacterObject> { custom.Base };

            TournamentParticipantPatch.FightTournamentGame_GetParticipantCharacters.Refill(
                settlement,
                result,
                removed
            );

            Tests.AssertEqual(2, result.Count, "The bracket was refilled to the original count.");
            Tests.AssertTrue(
                result.All(c => c != null && WCharacter.Get(c)?.IsCustom != true),
                "No custom troop was used for the refill."
            );
        }

        /// <summary>
        /// Kill-based unlock progress must weight items by the kill's loot multiplier (doctrine
        /// bonuses like Lions' Share double progress); a lost multiplier is invisible in game.
        /// </summary>
        [GameTest(
            "KillUnlockProgressWeighted",
            "regression",
            "Equipment accumulation weights each item by the kill's multiplier"
        )]
        public static void KillUnlockProgressWeighted(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var vanilla = CharacterObject.All.FirstOrDefault(c =>
                c != null && !c.IsHero && c.BattleEquipments.Any(e => e != null)
            );
            Tests.AssertNotNull(vanilla, "Found a vanilla troop with battle equipment.");

            var eq = new MEquipment(
                vanilla.BattleEquipments.First(),
                WCharacter.Get(vanilla)
            );

            var counts = new Dictionary<string, float>(StringComparer.Ordinal);
            UnlocksByKillsBehavior.AccumulateFromEquipment(eq, counts, 2f);

            Tests.AssertTrue(counts.Count > 0, "The loadout contributed at least one item.");
            Tests.AssertTrue(
                counts.Values.All(v => v > 0f && Math.Abs(v % 2f) < 0.001f),
                "Every accumulated count is a positive multiple of the ×2 weight."
            );
        }

        /// <summary>
        /// Pins the battle-rewards API for the current game version so a future rename fails
        /// loudly here instead of silently reading 0 renown/influence/morale/gold.
        /// </summary>
        [GameTest(
            "BattleRewardsSourceAvailable",
            "regression",
            "The API used to read battle rewards exists on this game version", RequiresCampaign = false)]
        public static void BattleRewardsSourceAvailable(GameTestContext ctx)
        {
#if BL14
            // Game 1.4+: rewards are read straight off the player's MapEventParty.
            var hasLegacy =
                typeof(MapEvent).GetMethod(
                    "GetBattleRewards",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                ) != null;

            var party = typeof(MapEventParty);
            var hasModern =
                party.GetProperty("GainedRenownExplained") != null
                && party.GetProperty("GainedInfluenceExplained") != null
                && party.GetProperty("GainedMoraleExplained") != null
                && party.GetProperty("PlunderedGold") != null
                && party.GetProperty("GoldLost") != null
                && typeof(MapEvent).GetMethod("PartiesOnSide") != null;

            Tests.AssertTrue(
                hasLegacy || hasModern,
                "A battle-rewards source (legacy MapEvent method or MapEventParty members) exists."
            );
#else
            Tests.AssertNotNull(
                typeof(MapEvent).GetMethod(
                    "GetBattleRewards",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                ),
                "MapEvent.GetBattleRewards exists on this game version."
            );
#endif
        }

        /// <summary>
        /// The clone path copies the tail of a character via FillFrom; game 1.4 moved it from
        /// CharacterObject to BasicCharacterObject. One of the two signatures must resolve.
        /// </summary>
        [GameTest(
            "CloneFillFromResolves",
            "regression",
            "A FillFrom overload (CharacterObject or BasicCharacterObject) exists for cloning", RequiresCampaign = false)]
        public static void CloneFillFromResolves(GameTestContext ctx)
        {
            var method =
                AccessTools.Method(
                    typeof(CharacterObject),
                    "FillFrom",
                    [typeof(CharacterObject)]
                )
                ?? AccessTools.Method(
                    typeof(CharacterObject),
                    "FillFrom",
                    [typeof(BasicCharacterObject)]
                );

            Tests.AssertNotNull(method, "A FillFrom overload resolved on this game version.");
        }

        /// <summary>
        /// Crafting a new smithy item must invalidate the static item caches so the editor's
        /// equipment lists pick it up without a restart, and the caches must rebuild afterward.
        /// </summary>
        [GameTest(
            "CraftedItemClearsItemCaches",
            "regression",
            "The crafted-item listener clears the WItem caches and they rebuild"
        )]
        public static void CraftedItemClearsItemCaches(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var before = WItem.Equipments.Count;
            Tests.AssertTrue(before > 0, "The equipment cache is populated.");

            var anyItem = WItem.All.FirstOrDefault()?.Base;
            CraftedItemsCacheBehavior.OnNewItemCrafted(anyItem, null, true);

            var after = WItem.Equipments.Count;
            Tests.AssertTrue(after > 0, "The equipment cache rebuilt after invalidation.");
            Tests.AssertEqual(before, after, "Rebuild yields the same item set.");
        }

        /// <summary>
        /// The reset_doctrines console command must clear doctrine and feat progress.
        /// </summary>
        [GameTest(
            "ResetDoctrinesCommandClearsProgress",
            "regression",
            "retinues.reset_doctrines clears acquired doctrines and feat progress"
        )]
        public static void ResetDoctrinesCommandClearsProgress(GameTestContext ctx)
        {
            DoctrinesRegistry.EnsureRegistered();

            var feat = DoctrinesRegistry.GetFeats().FirstOrDefault();
            Tests.AssertNotNull(feat, "At least one feat is registered.");

            // The command resets process-global singletons; snapshot everything and restore so
            // a live campaign's doctrine progress is not wiped by running the suite.
            var doctrineSnapshot = DoctrinesRegistry
                .GetDoctrines()
                .ToDictionary(d => d.Id, d => (d.IsAcquired, d.Progress));
            var featSnapshot = DoctrinesRegistry.GetFeats().ToDictionary(f => f.Id, f => f.Progress);

            try
            {
                feat.ForceSet(Math.Max(1, feat.Progress + 1));
                Tests.AssertTrue(feat.Progress > 0, "Feat progress was set (precondition).");

                DoctrinesRegistry.ResetDoctrines([]);

                Tests.AssertEqual(0, feat.Progress, "The command reset the feat's progress.");
                Tests.AssertTrue(
                    DoctrinesRegistry.GetDoctrines().All(d => !d.IsAcquired && d.Progress == 0),
                    "All doctrines are unacquired with zero progress after the reset."
                );
            }
            finally
            {
                foreach (var d in DoctrinesRegistry.GetDoctrines())
                {
                    if (!doctrineSnapshot.TryGetValue(d.Id, out var snap))
                        continue;
                    d.ForceSet(snap.Progress);
                    d.IsAcquired = snap.IsAcquired;
                }

                foreach (var f in DoctrinesRegistry.GetFeats())
                {
                    if (featSnapshot.TryGetValue(f.Id, out var progress))
                        f.ForceSet(progress);
                }
            }
        }

        /// <summary>
        /// Smithed weapons must stay in the crafted equipment list across a restart. The engine's
        /// IsCraftedByPlayer flag is runtime-only and is not restored for weapons made through
        /// third-party smithing mods, so the persisted crafted-item id is accepted as well - but
        /// vanilla's XML pre-crafted weapons (which also carry a WeaponDesign) must stay out.
        /// </summary>
        [GameTest(
            "CraftedDetectionSurvivesRestart",
            "regression",
            "Crafted weapons are detected by their persisted id, and vanilla pre-crafted weapons are not"
        )]
        public static void CraftedDetectionSurvivesRestart(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            foreach (var item in WItem.All)
            {
                if (item?.Base?.WeaponDesign == null)
                    continue;

                if (item.StringId?.StartsWith("crafted_item_") == true)
                {
                    Tests.AssertTrue(
                        item.IsCrafted,
                        $"Smithed weapon '{item.StringId}' is detected as crafted."
                    );
                }
                else if (!item.Base.IsCraftedByPlayer)
                {
                    Tests.AssertFalse(
                        item.IsCrafted,
                        $"Vanilla pre-crafted weapon '{item.StringId}' is not treated as player-crafted."
                    );
                }
            }
        }
    }
}
