using System;
using System.Collections.Generic;
using System.Linq;

namespace Retinues.Core.Utils
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                   Similarity  Helpers                  //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Helpers for calculating set and vector similarity metrics.
    /// Used for fuzzy matching and comparison of collections.
    /// </summary>
    public static class Similarity
    {
        /// <summary>
        /// Computes the Jaccard similarity between two sets of strings.
        /// Returns 1.0 for identical/empty sets, 0.0 for disjoint sets.
        /// </summary>
        public static double Jaccard(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 && b.Count == 0)
                return 1.0;
            if (a.Count == 0 || b.Count == 0)
                return 0.0;
            int inter = a.Count(s => b.Contains(s));
            int union = a.Count + b.Count - inter;
            return union == 0 ? 0.0 : (double)inter / union;
        }

        /// <summary>
        /// Computes the cosine similarity between two string→int vectors.
        /// Returns 1.0 for identical/empty, 0.0 for orthogonal.
        /// </summary>
        public static double Cosine(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            if (a.Count == 0 && b.Count == 0)
                return 1.0;
            if (a.Count == 0 || b.Count == 0)
                return 0.0;

            long dot = 0;
            long na2 = 0;
            long nb2 = 0;

            foreach (var kv in a)
            {
                var ai = (long)kv.Value;
                na2 += ai * ai;
                if (b.TryGetValue(kv.Key, out var bj))
                    dot += ai * bj;
            }
            foreach (var v in b.Values)
            {
                var bj = (long)v;
                nb2 += bj * bj;
            }

            if (na2 == 0 || nb2 == 0)
                return 0.0;
            return dot / (Math.Sqrt(na2) * Math.Sqrt(nb2));
        }
    }
}
