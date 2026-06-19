using System.Linq;
using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>Tests for the upgrade-tree model (DAG with reverse edges).</summary>
    public static class TreeTests
    {
        [GameTest(
            "UpgradeTargetsAndReverseEdges",
            "tree",
            "Upgrade targets round-trip and produce reverse-edge upgrade sources"
        )]
        public static void UpgradeTargetsAndReverseEdges(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Tests.AssertNotNull(looter, "A vanilla 'looter' troop exists to clone from.");

            var parent = sandbox.Track(CharacterCloner.Clone(WCharacter.Get(looter)));
            var child = sandbox.Track(CharacterCloner.Clone(WCharacter.Get(looter)));

            parent.UpgradeTargets = [child];

            Tests.AssertEqual(1, parent.UpgradeTargets.Count, "Parent has one upgrade target.");
            Tests.AssertEqual(
                child.StringId,
                parent.UpgradeTargets[0].StringId,
                "Parent's upgrade target is the child."
            );

            // The tree cache exposes the reverse edge.
            Tests.AssertTrue(
                child.UpgradeSources.Any(s => s.StringId == parent.StringId),
                "Child's upgrade sources include the parent (DAG reverse edge)."
            );
        }

        [GameTest(
            "DetachRemovesFromAllSources",
            "tree",
            "Removing a troop detaches it from every parent that lists it"
        )]
        public static void DetachRemovesFromAllSources(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Tests.AssertNotNull(looter, "A vanilla 'looter' troop exists to clone from.");

            // Two parents both upgrading into the same child (DAG / multi-path).
            var parentA = sandbox.Track(CharacterCloner.Clone(WCharacter.Get(looter)));
            var parentB = sandbox.Track(CharacterCloner.Clone(WCharacter.Get(looter)));
            var child = sandbox.Track(CharacterCloner.Clone(WCharacter.Get(looter)));

            parentA.UpgradeTargets = [child];
            parentB.UpgradeTargets = [child];

            child.Remove();

            Tests.AssertFalse(
                parentA.UpgradeTargets.Any(t => t.StringId == child.StringId),
                "Removed child is detached from parent A."
            );
            Tests.AssertFalse(
                parentB.UpgradeTargets.Any(t => t.StringId == child.StringId),
                "Removed child is detached from parent B (no leftover ghost)."
            );
        }
    }
}
