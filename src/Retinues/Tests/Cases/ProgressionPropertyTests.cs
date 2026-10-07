using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Experience;
using Retinues.Behaviors.Retinues.Patches;
using Retinues.Editor.MVC.Pages.Equipment.Services;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Tests.Cases
{
    public static class ProgressionPropertyTests
    {
        [GameTest("MixedXpConservesNormalizedProgress", "properties", RequiresCampaign = false)]
        public static void MixedXpConservesNormalizedProgress(GameTestContext ctx)
        {
            for (int trial = 0; trial < 200; trial++)
                ctx.Case(trial, () =>
                {
                    var grants = new List<(int Xp, int Cost)>();
                    decimal expected = 0;
                    for (int i = 0; i < 40; i++)
                    {
                        int cost = ctx.Random.Next(100, 100000);
                        int xp = ctx.Random.Next(0, cost * 3);
                        grants.Add((xp, cost));
                        expected += (decimal)xp / cost;
                    }
                    double progress = 0;
                    int points = 0;
                    foreach (var grant in grants)
                    {
                        points += SkillPointProgress.Add(ref progress, grant.Xp, grant.Cost);
                        Tests.AssertTrue(progress >= 0 && progress < 1, "The shared remainder stays fractional.");
                    }
                    Tests.AssertEqual((int)decimal.Floor(expected), points, "Awards match an independent decimal oracle.");
                    Tests.AssertTrue(Math.Abs(points + progress - (double)expected) < 1e-8, "No progress is created or lost.");
                    double reverseProgress = 0;
                    int reversePoints = 0;
                    grants.Reverse();
                    foreach (var grant in grants)
                        reversePoints += SkillPointProgress.Add(ref reverseProgress, grant.Xp, grant.Cost);
                    Tests.AssertEqual(points, reversePoints, "Award order cannot change whole points.");
                    Tests.AssertTrue(Math.Abs(progress - reverseProgress) < 1e-8);
                });
        }

        [GameTest("CraftedDesignGroupingPreservesEveryOwnedCopy", "properties", RequiresCampaign = false)]
        public static void CraftedDesignGroupingPreservesEveryOwnedCopy(GameTestContext ctx)
        {
            for (int trial = 0; trial < 200; trial++)
                ctx.Case(trial, () =>
                {
                    var items = Enumerable.Range(0, ctx.Random.Next(1, 160))
                        .Select(id => (Id: id, Design: ctx.Random.Next(0, 8), Owned: ctx.Random.Next(4) == 0)).ToList();
                    var selected = CraftedItemSelection.Select(items,
                        item => item.Design == 0 ? null : item.Design.ToString(), item => item.Owned);
                    Tests.AssertEqual(selected.Count, selected.Select(i => i.Id).Distinct().Count(), "No item ID is duplicated.");
                    Tests.AssertTrue(items.Where(i => i.Owned || i.Design == 0).All(i => selected.Contains(i)),
                        "Stocked/equipped IDs and ordinary items remain selectable.");
                    foreach (var design in items.Where(i => i.Design != 0).GroupBy(i => i.Design))
                        Tests.AssertEqual(Math.Max(1, design.Count(i => i.Owned)), selected.Count(i => i.Design == design.Key),
                            "An unowned design has one representative; owned copies retain their identity.");
                    Tests.AssertTrue(selected.All(items.Contains), "Grouping cannot invent equipment.");
                });
        }

        [GameTest("RetinueCommandSequencesNeverOvershoot", "properties", RequiresCampaign = false)]
        public static void RetinueCommandSequencesNeverOvershoot(GameTestContext ctx)
        {
            for (int trial = 0; trial < 200; trial++)
                ctx.Case(trial, () =>
                {
                    int cap = ctx.Random.Next(0, 150), count = ctx.Random.Next(0, cap + 1);
                    for (int step = 0; step < 30; step++)
                    {
                        count = Math.Max(0, count - ctx.Random.Next(0, 5)); // dismiss/transfer out
                        int requested = new[] { 1, 5, 100 }[ctx.Random.Next(3)];
                        int room = cap - count;
                        var command = new PartyScreenLogic.PartyCommand();
                        command.FillForUpgradeTroop(PartyScreenLogic.PartyRosterSide.Right,
                            PartyScreenLogic.TroopType.Member, null, requested, 2, 4);
                        bool execute = RetinueDynamicUpgradePatch.ClampUpgradeCommand(command, room);
                        Tests.AssertEqual(room > 0, execute);
                        if (execute)
                        {
                            Tests.AssertEqual(Math.Min(room, requested), command.TotalNumber);
                            count += command.TotalNumber;
                        }
                        Tests.AssertTrue(count <= cap, "Every command respects current capacity.");
                        Tests.AssertEqual(2, command.UpgradeTarget, "The chosen upgrade remains unchanged.");
                    }
                });
        }
    }
}
