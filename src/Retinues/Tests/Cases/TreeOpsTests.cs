using System;
using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Game.Wrappers;
using Retinues.Managers;
using Retinues.Troops;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for troop tree operations: upgrade-slot limits, max-tier gating, and deletability.
    /// All operations happen on a throwaway sandbox faction.
    /// </summary>
    public static class TreeOpsTests
    {
        /// <summary>
        /// A basic troop accepts up to Config.MaxBasicUpgrades upgrade targets and no more.
        /// </summary>
        [GameTest(
            "AddUpgradeRespectsBasicSlotLimit",
            "troops",
            "Basic troops accept up to the configured number of upgrade slots"
        )]
        public static void AddUpgradeRespectsBasicSlotLimit(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");

            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);
            var root = faction.RootBasic;
            Tests.AssertNotNull(root, "Basic root exists.");
            Tests.AssertFalse(root.IsMaxTier, "Basic root is below max tier.");

            int limit = Math.Min((int)Config.MaxBasicUpgrades, 4);
            Tests.AssertTrue(limit >= 1, "Configured basic upgrade limit is at least 1.");

            for (int i = 0; i < limit; i++)
            {
                Tests.AssertTrue(
                    UpgradeManager.CanAddUpgradeToTroop(root),
                    $"Can add upgrade #{i + 1} of {limit}."
                );
                UpgradeManager.AddUpgradeTarget(root, $"Test Basic Upgrade {i + 1}");
            }

            Tests.AssertFalse(
                UpgradeManager.CanAddUpgradeToTroop(root),
                $"Cannot exceed the basic upgrade limit ({limit})."
            );
        }

        /// <summary>
        /// An elite troop accepts Config.MaxEliteUpgrades upgrade targets (+1 with Masters-at-Arms,
        /// capped at 4).
        /// </summary>
        [GameTest(
            "AddUpgradeRespectsEliteSlotLimit",
            "troops",
            "Elite troops accept the configured slots (+1 with Masters-at-Arms, capped at 4)"
        )]
        public static void AddUpgradeRespectsEliteSlotLimit(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");

            TroopBuilder.CreateTroops(faction, isElite: true, copyWholeTree: false);
            var root = faction.RootElite;
            Tests.AssertNotNull(root, "Elite root exists.");
            Tests.AssertFalse(root.IsMaxTier, "Elite root is below max tier.");

            int limit = (int)Config.MaxEliteUpgrades;
            if (DoctrineAPI.IsDoctrineUnlocked<MastersAtArms>())
                limit += 1;
            limit = Math.Min(limit, 4);
            Tests.AssertTrue(limit >= 1, "Configured elite upgrade limit is at least 1.");

            for (int i = 0; i < limit; i++)
            {
                Tests.AssertTrue(
                    UpgradeManager.CanAddUpgradeToTroop(root),
                    $"Can add upgrade #{i + 1} of {limit}."
                );
                UpgradeManager.AddUpgradeTarget(root, $"Test Elite Upgrade {i + 1}");
            }

            Tests.AssertFalse(
                UpgradeManager.CanAddUpgradeToTroop(root),
                $"Cannot exceed the elite upgrade limit ({limit})."
            );
        }

        /// <summary>
        /// A troop at max tier cannot add upgrade targets.
        /// </summary>
        [GameTest(
            "MaxTierBlocksUpgrade",
            "troops",
            "A max-tier troop cannot add upgrade targets"
        )]
        public static void MaxTierBlocksUpgrade(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");

            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);
            var root = faction.RootBasic;
            Tests.AssertNotNull(root, "Basic root exists.");

            // Push the troop to max tier (Tier derives from Level).
            root.Level = 62;
            Tests.AssertTrue(root.IsMaxTier, $"Troop is at max tier (tier {root.Tier}).");

            Tests.AssertFalse(
                UpgradeManager.CanAddUpgradeToTroop(root),
                "A max-tier troop must not accept upgrades."
            );
        }

        /// <summary>
        /// Roots and parents-with-children are not deletable; leaf regular troops are.
        /// </summary>
        [GameTest(
            "IsDeletableGating",
            "troops",
            "Roots and parents-with-children are not deletable; leaf regulars are"
        )]
        public static void IsDeletableGating(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");

            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);
            var root = faction.RootBasic;
            Tests.AssertNotNull(root, "Basic root exists.");

            Tests.AssertFalse(root.IsDeletable, "Root troop is not deletable.");

            var child = UpgradeManager.AddUpgradeTarget(root, "Test Leaf");
            Tests.AssertTrue(child.IsDeletable, "Leaf child (regular, no upgrades) is deletable.");

            var grandchild = UpgradeManager.AddUpgradeTarget(child, "Test Grandleaf");
            Tests.AssertFalse(child.IsDeletable, "A child with its own upgrade is not deletable.");
            Tests.AssertTrue(grandchild.IsDeletable, "The grandchild leaf is deletable.");
        }

        /// <summary>
        /// Removing a troop that is referenced by multiple parents (a DAG-shaped tree, as produced
        /// by reworks like RBM) strips it from every parent — no undeletable "ghost" is left.
        /// </summary>
        [GameTest(
            "RemoveDetachesAllParents",
            "troops",
            "Removing a multi-parent troop strips it from every parent's upgrade targets"
        )]
        public static void RemoveDetachesAllParents(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);
            var root = faction.RootBasic;
            Tests.AssertNotNull(root, "Basic root exists.");

            // Two parents, plus a child whose tracked (UpgradeMap) parent is p1.
            var p1 = UpgradeManager.AddUpgradeTarget(root, "Parent One");
            var p2 = UpgradeManager.AddUpgradeTarget(root, "Parent Two");
            var child = UpgradeManager.AddUpgradeTarget(p1, "Shared Child");

            // Simulate a DAG: list the same child under a second parent (what RBM-style trees do).
            p2.UpgradeTargets = [.. p2.UpgradeTargets, child];

            Tests.AssertTrue(
                p1.UpgradeTargets.Any(t => t.StringId == child.StringId),
                "Child is under the first parent."
            );
            Tests.AssertTrue(
                p2.UpgradeTargets.Any(t => t.StringId == child.StringId),
                "Child is under the second parent."
            );

            child.Remove();

            Tests.AssertFalse(
                p1.UpgradeTargets.Any(t => t.StringId == child.StringId),
                "Child was stripped from the first parent."
            );
            Tests.AssertFalse(
                p2.UpgradeTargets.Any(t => t.StringId == child.StringId),
                "Child was stripped from the second parent (no ghost left)."
            );
            Tests.AssertFalse(
                WCharacter.ActiveStubIds.Contains(child.StringId),
                "The child's stub was freed."
            );
        }
    }
}
