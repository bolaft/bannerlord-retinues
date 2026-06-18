using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Troops;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for TroopBuilder cloning and troop classification. All tree-building happens on a
    /// throwaway sandbox faction so the player's real troops are never touched.
    /// </summary>
    public static class TroopBuilderTests
    {
        /// <summary>
        /// Cloning the whole culture tree produces more than one custom troop, all bound to the
        /// faction.
        /// </summary>
        [GameTest(
            "CloneWholeTreeProducesTree",
            "troops",
            "CreateTroops(copyWholeTree:true) clones a multi-troop tree bound to the faction"
        )]
        public static void CloneWholeTreeProducesTree(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");

            TroopBuilder.CreateTroops(faction, isElite: true, copyWholeTree: true);

            var root = faction.RootElite;
            Tests.AssertNotNull(root, "Elite root was created.");

            var tree = root.Tree.ToList();
            Tests.AssertTrue(
                tree.Count > 1,
                $"Whole-tree clone produced more than one troop (got {tree.Count})."
            );

            foreach (var troop in tree)
            {
                Tests.AssertTrue(troop.IsCustom, $"Cloned troop '{troop.Name}' is custom.");
                Tests.AssertTrue(
                    troop.Faction == faction,
                    $"Cloned troop '{troop.Name}' is bound to the faction."
                );
            }
        }

        /// <summary>
        /// Cloning without the whole tree produces exactly one root troop.
        /// </summary>
        [GameTest(
            "CloneSingleRootWhenNotWholeTree",
            "troops",
            "CreateTroops(copyWholeTree:false) clones only the root troop"
        )]
        public static void CloneSingleRootWhenNotWholeTree(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");

            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);

            var root = faction.RootBasic;
            Tests.AssertNotNull(root, "Basic root was created.");
            Tests.AssertEqual(1, root.Tree.Count(), "Single-root clone produced exactly one troop.");
        }

        /// <summary>
        /// Tiers are non-decreasing from a parent to each of its upgrade targets.
        /// </summary>
        [GameTest(
            "ClonedTreeTiersNonDecreasing",
            "troops",
            "Each upgrade target's tier is greater than or equal to its parent's tier"
        )]
        public static void ClonedTreeTiersNonDecreasing(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");

            TroopBuilder.CreateTroops(faction, isElite: true, copyWholeTree: true);

            var root = faction.RootElite;
            Tests.AssertNotNull(root, "Elite root was created.");

            foreach (var troop in root.Tree)
            foreach (var child in troop.UpgradeTargets)
            {
                Tests.AssertTrue(
                    child.Tier >= troop.Tier,
                    $"Child '{child.Name}' tier {child.Tier} >= parent '{troop.Name}' tier {troop.Tier}."
                );
            }
        }

        /// <summary>
        /// Elite-tree troops classify as Elite + Regular; basic-tree troops as Regular but not
        /// Elite. Neither is a retinue.
        /// </summary>
        [GameTest(
            "ClassificationEliteVsBasic",
            "troops",
            "Elite-tree troops are Elite+Regular; basic-tree troops are Regular, not Elite"
        )]
        public static void ClassificationEliteVsBasic(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");

            TroopBuilder.CreateTroops(faction, isElite: true, copyWholeTree: false);
            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);

            var elite = faction.RootElite;
            var basic = faction.RootBasic;
            Tests.AssertNotNull(elite, "Elite root exists.");
            Tests.AssertNotNull(basic, "Basic root exists.");

            Tests.AssertTrue(elite.IsRegular, "Elite root is a regular troop.");
            Tests.AssertTrue(elite.IsElite, "Elite root is an elite troop.");
            Tests.AssertFalse(elite.IsRetinue, "Elite root is not a retinue.");

            Tests.AssertTrue(basic.IsRegular, "Basic root is a regular troop.");
            Tests.AssertFalse(basic.IsElite, "Basic root is not an elite troop.");
            Tests.AssertFalse(basic.IsRetinue, "Basic root is not a retinue.");
        }

        /// <summary>
        /// A created troop is bound to its faction; removing it frees the stub and clears the
        /// faction binding.
        /// </summary>
        [GameTest(
            "FactionBindingAndRelease",
            "troops",
            "A created troop binds to its faction and frees its stub when removed"
        )]
        public static void FactionBindingAndRelease(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");

            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);

            var root = faction.RootBasic;
            Tests.AssertNotNull(root, "Basic root exists.");
            var id = root.StringId;

            Tests.AssertTrue(root.Faction == faction, "Troop is bound to the faction.");
            Tests.AssertTrue(WCharacter.ActiveStubIds.Contains(id), "Troop stub is active.");

            root.Remove();

            Tests.AssertFalse(WCharacter.ActiveStubIds.Contains(id), "Stub freed after removal.");
            Tests.AssertTrue(root.Faction == null, "Faction cleared after removal.");
        }
    }
}
