using System.Linq;
using Retinues.Managers;
using Retinues.Troops;
using Retinues.Troops.Save;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// End-to-end integration: clone a troop tree, edit it (add an upgrade, set a skill, rename),
    /// serialize the whole faction, mutate the live troops, then re-apply to a fresh faction and
    /// assert the edits survive. Combines TroopBuilder + UpgradeManager + skills + save.
    /// </summary>
    public static class E2ETests
    {
        [GameTest(
            "EditCloneSaveReapplyIntact",
            "e2e",
            "Clone -> edit (upgrade + skill + rename) -> serialize -> re-apply keeps edits intact"
        )]
        public static void EditCloneSaveReapplyIntact(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var source = sandbox.NewFaction();
            Tests.AssertNotNull(source, "A non-player faction with troop roots is available.");
            var target = sandbox.NewFaction(source);
            Tests.AssertNotNull(target, "A second, distinct non-player faction is available.");

            // Clone a single elite root, then edit: add a customized upgrade child.
            TroopBuilder.CreateTroops(source, isElite: true, copyWholeTree: false);
            var root = source.RootElite;
            Tests.AssertNotNull(root, "Elite root was created.");

            var child = UpgradeManager.AddUpgradeTarget(root, "Bespoke Guard");
            Tests.AssertNotNull(child, "Upgrade child was created.");

            var skill = child.AllSkills.FirstOrDefault();
            Tests.AssertNotNull(skill, "The child has at least one skill.");

            const int expectedSkill = 137;
            const string expectedName = "Bespoke Guard";
            child.SetSkill(skill, expectedSkill);
            var childId = child.StringId;

            // Serialize the whole faction, then mutate the live troops to prove restoration.
            var data = new FactionSaveData(source);
            child.Name = "MUTATED";
            child.SetSkill(skill, 1);

            // Re-apply to a fresh faction (the load path).
            data.Apply(target);

            var rebuiltRoot = target.RootElite;
            Tests.AssertNotNull(rebuiltRoot, "Elite root rebuilt on the target faction.");
            Tests.AssertEqual(2, rebuiltRoot.Tree.Count(), "Tree shape preserved (root + child).");

            var rebuiltChild = rebuiltRoot.Tree.FirstOrDefault(t => t.StringId == childId);
            Tests.AssertNotNull(rebuiltChild, "The edited child was rebuilt.");
            Tests.AssertEqual(expectedName, rebuiltChild.Name, "The child's custom name survived.");
            Tests.AssertEqual(
                expectedSkill,
                rebuiltChild.GetSkill(skill),
                "The child's edited skill survived."
            );
        }
    }
}
