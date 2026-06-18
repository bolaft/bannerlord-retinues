using System;
using System.Collections.Generic;
using Retinues.Utils;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the pure utility helpers (no game state). These are exact-value tests and are the
    /// best candidates for an off-game xUnit project if one is ever added.
    /// </summary>
    public static class UtilsTests
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Similarity                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Jaccard similarity over string sets.
        /// </summary>
        [GameTest("JaccardSimilarity", "utils", "Jaccard similarity over string sets")]
        public static void JaccardSimilarity()
        {
            Tests.AssertTrue(Eq(1.0, Similarity.Jaccard(Set(), Set())), "empty vs empty is 1.");
            Tests.AssertTrue(
                Eq(0.0, Similarity.Jaccard(Set("a"), Set())),
                "non-empty vs empty is 0."
            );
            Tests.AssertTrue(
                Eq(1.0, Similarity.Jaccard(Set("a", "b"), Set("a", "b"))),
                "identical sets are 1."
            );
            Tests.AssertTrue(
                Eq(0.0, Similarity.Jaccard(Set("a"), Set("b"))),
                "disjoint sets are 0."
            );
            Tests.AssertTrue(
                Eq(1.0 / 3.0, Similarity.Jaccard(Set("a", "b"), Set("b", "c"))),
                "one shared of three is 1/3."
            );
        }

        /// <summary>
        /// Cosine similarity over string→int vectors.
        /// </summary>
        [GameTest("CosineSimilarity", "utils", "Cosine similarity over string->int vectors")]
        public static void CosineSimilarity()
        {
            Tests.AssertTrue(Eq(1.0, Similarity.Cosine(Vec(), Vec())), "empty vs empty is 1.");
            Tests.AssertTrue(
                Eq(0.0, Similarity.Cosine(Vec(("a", 1)), Vec())),
                "non-empty vs empty is 0."
            );
            Tests.AssertTrue(
                Eq(1.0, Similarity.Cosine(Vec(("a", 3), ("b", 4)), Vec(("a", 3), ("b", 4)))),
                "identical vectors are 1."
            );
            Tests.AssertTrue(
                Eq(0.0, Similarity.Cosine(Vec(("a", 1)), Vec(("b", 1)))),
                "orthogonal vectors are 0."
            );
            Tests.AssertTrue(
                Eq(1.0 / Math.Sqrt(2.0), Similarity.Cosine(Vec(("a", 1)), Vec(("a", 1), ("b", 1)))),
                "partial overlap is 1/sqrt(2)."
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Format                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Format helpers: thousands separator, cropping, and camelCase-to-title.
        /// </summary>
        [GameTest("FormatHelpers", "utils", "Number grouping, cropping, and camelCase titling")]
        public static void FormatHelpers()
        {
            Tests.AssertEqual("1 234 567", Format.Number(1234567), "thousands grouped with spaces.");

            Tests.AssertEqual("abcde(...)", Format.Crop("abcdefghij", 5), "crop appends a marker.");
            Tests.AssertEqual("short", Format.Crop("short", 10), "no crop within the limit.");

            var title = Format.CamelCaseToTitle("maxEliteRetinueRatio");
            Tests.AssertTrue(title.Contains(" "), "camelCase gains spaces.");
            Tests.AssertEqual(4, title.Split(' ').Length, "four words produced.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool Eq(double a, double b) => Math.Abs(a - b) < 1e-9;

        private static HashSet<string> Set(params string[] items) => new(items);

        private static Dictionary<string, int> Vec(params (string key, int value)[] items)
        {
            var d = new Dictionary<string, int>();
            foreach (var (key, value) in items)
                d[key] = value;
            return d;
        }
    }
}
