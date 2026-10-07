using System;
using System.Linq;
using Retinues.Utils;

namespace Retinues.Tests.Cases
{
    public static class SimilarityPropertyTests
    {
        [GameTest("EquipmentSimilarityIsSymmetricAndBounded", "properties", RequiresCampaign = false)]
        public static void EquipmentSimilarityIsSymmetricAndBounded(GameTestContext ctx)
        {
            for (int trial = 0; trial < 500; trial++)
                ctx.Case(trial, () =>
                {
                    var a = Enumerable.Range(0, 20).Where(_ => ctx.Random.Next(2) == 0)
                        .ToDictionary(i => "item_" + i, _ => ctx.Random.Next(1, 100001));
                    var b = Enumerable.Range(0, 20).Where(_ => ctx.Random.Next(2) == 0)
                        .ToDictionary(i => "item_" + i, _ => ctx.Random.Next(1, 100001));
                    var aSet = a.Keys.ToHashSet();
                    var bSet = b.Keys.ToHashSet();
                    double cosine = Similarity.Cosine(a, b);
                    double jaccard = Similarity.Jaccard(aSet, bSet);
                    Tests.AssertTrue(cosine >= -1e-12 && cosine <= 1 + 1e-12);
                    Tests.AssertTrue(jaccard >= 0 && jaccard <= 1);
                    Tests.AssertTrue(Math.Abs(cosine - Similarity.Cosine(b, a)) < 1e-12);
                    Tests.AssertEqual(jaccard, Similarity.Jaccard(bSet, aSet));
                    int union = aSet.Union(bSet).Count();
                    double expected = union == 0 ? 1 : (double)aSet.Intersect(bSet).Count() / union;
                    Tests.AssertEqual(expected, jaccard, "Independent set-intersection oracle.");
                    Tests.AssertTrue(Math.Abs(1 - Similarity.Cosine(a, a)) < 1e-12);
                    var scaled = a.ToDictionary(p => p.Key, p => p.Value * 2);
                    Tests.AssertTrue(Math.Abs(cosine - Similarity.Cosine(scaled, b)) < 1e-12, "Changing scale cannot change equipment similarity.");
                });
        }
    }
}
