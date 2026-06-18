using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for troop matching and roster swapping. Swaps run against throwaway dummy rosters and
    /// sandbox factions so the player's real party is never touched.
    /// </summary>
    public static class SwapsTests
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Matching                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// PickBestFromTree returns a same-tier custom candidate, and excluding a troop removes it
        /// from the candidate pool.
        /// </summary>
        [GameTest(
            "PickBestFromTreeMatchesTier",
            "swaps",
            "PickBestFromTree returns a same-tier custom candidate and honors exclude"
        )]
        public static void PickBestFromTreeMatchesTier(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: true, copyWholeTree: true);

            var root = faction.RootElite;
            Tests.AssertNotNull(root, "Elite root exists.");
            var tree = root.Tree.ToList();
            Tests.AssertTrue(tree.Count > 1, "The tree has multiple troops.");

            var target = tree.First(t => t.StringId != root.StringId);

            var match = TroopMatcher.PickBestFromTree(root, target, sameTierOnly: true);
            Tests.AssertNotNull(match, "Found a same-tier match.");
            Tests.AssertEqual(target.Tier, match.Tier, "Match has the same tier.");
            Tests.AssertTrue(match.IsCustom, "Match is a custom troop.");

            var excluded = TroopMatcher.PickBestFromTree(
                root,
                target,
                exclude: target,
                sameTierOnly: true
            );
            if (excluded != null)
                Tests.AssertTrue(
                    excluded.StringId != target.StringId,
                    "Excluding the target removes it from the candidates."
                );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Roster swaps                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Hero-safe swap replaces non-hero troops with the faction's custom equivalents, keeps
        /// heroes untouched, and preserves the total head count.
        /// </summary>
        [GameTest(
            "RosterSwapPreservesHeroesAndCount",
            "swaps",
            "Hero-safe swap replaces troops with custom equivalents, keeps heroes, preserves count"
        )]
        public static void RosterSwapPreservesHeroesAndCount(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: true);

            var vanilla = faction.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "A vanilla template troop is available.");

            var roster = new WRoster(TroopRoster.CreateDummyTroopRoster(), Player.Party);
            roster.AddTroop(vanilla, 5);
            roster.AddTroop(Player.Character, 1); // hero
            bool heroAdded = roster.CountOf(Player.Character) == 1;

            Tests.AssertEqual(5, roster.CountOf(vanilla), "Vanilla troops were added.");
            Tests.AssertEqual(0, roster.CustomCount, "No custom troops before the swap.");

            roster.SwapTroopsPreservingHeroes(faction);

            Tests.AssertEqual(0, roster.CountOf(vanilla), "Vanilla troops were swapped out.");
            Tests.AssertTrue(roster.CustomCount >= 5, "Troops were replaced by custom equivalents.");

            if (heroAdded)
                Tests.AssertEqual(1, roster.CountOf(Player.Character), "The hero was preserved.");

            Tests.AssertEqual(
                5 + (heroAdded ? 1 : 0),
                roster.HealthyCount,
                "Total head count is preserved."
            );
        }

        /// <summary>
        /// SwapTroop replaces one troop with another, preserving the stack count.
        /// </summary>
        [GameTest(
            "SwapTroopReplacesPreservingCount",
            "swaps",
            "SwapTroop replaces a troop with another and preserves the stack count"
        )]
        public static void SwapTroopReplacesPreservingCount(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);

            var custom = faction.RootBasic;
            var vanilla = faction.Culture?.RootBasic;
            Tests.AssertNotNull(custom, "Custom basic root exists.");
            Tests.AssertNotNull(vanilla, "Vanilla template exists.");

            var roster = new WRoster(TroopRoster.CreateDummyTroopRoster(), Player.Party);
            roster.AddTroop(vanilla, 7);

            roster.SwapTroop(vanilla, custom);

            Tests.AssertEqual(0, roster.CountOf(vanilla), "The old troop was removed.");
            Tests.AssertEqual(7, roster.CountOf(custom), "The new troop has the same count.");
            Tests.AssertEqual(7, roster.HealthyCount, "Total head count is preserved.");
        }
    }
}
