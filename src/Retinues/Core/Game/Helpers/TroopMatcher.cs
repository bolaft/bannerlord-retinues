using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Helpers
{
    [SafeClass]
    public static class TroopMatcher
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Weights                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const int WEIGHT_MOUNTED = 1000000;
        private const int WEIGHT_RANGED = 100000;
        private const int WEIGHT_FEMALE = 10000;
        private const double WEIGHT_WEAP = 1.0; // weapon Jaccard gets multiplied by this * 1000
        private const double WEIGHT_SKILL = 1.0; // skill cosine gets multiplied by this * 1000

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Matching                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WCharacter PickBestFromFaction(WFaction faction, WCharacter troop)
        {
            if (faction == null || troop == null || !troop.IsValid)
                return null;

            var root = troop.IsElite ? faction.RootElite : faction.RootBasic;
            if (root == null || !root.IsValid)
                return null;

            return PickBestFromTree(root, troop);
        }

        public static WCharacter PickBestFromTree(
            WCharacter root,
            WCharacter troop,
            WCharacter exclude = null
        )
        {
            if (!root.IsValid)
                return null;

            // Hard filter by Tier
            var candidates =
                root.Tree?.Where(t =>
                        t.IsValid
                        && t.Tier == troop.Tier
                        && (exclude == null || t.StringId != exclude.StringId)
                    )
                    .ToList() ?? [];

            if (candidates.Count == 0)
                return null;

            // Rank by score; stable tie-break by StringId
            var best = candidates
                .Select(t => new { Troop = t, Score = EligibilityScore(t, troop) })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Troop.StringId, StringComparer.Ordinal)
                .First()
                .Troop;

            return best;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static int EligibilityScore(WCharacter troop, WCharacter retinue)
        {
            int score = 0;

            // 1) Mounted match (strongest)
            if (troop.IsMounted == retinue.IsMounted)
                score += WEIGHT_MOUNTED;

            // 2) Ranged match
            if (troop.IsRanged == retinue.IsRanged)
                score += WEIGHT_RANGED;

            // 3) Female match
            if (troop.IsFemale == retinue.IsFemale)
                score += WEIGHT_FEMALE;

            // 4) Weapon classes similarity (Jaccard)
            var w1 = SafeWeaponClasses(troop);
            var w2 = SafeWeaponClasses(retinue);
            double jacc = Similarity.Jaccard(w1, w2);
            score += (int)Math.Round(jacc * 1000.0 * WEIGHT_WEAP);

            // 5) Skillset similarity (cosine on shared keys)
            var s1 = SafeSkills(troop);
            var s2 = SafeSkills(retinue);
            double cos = Similarity.Cosine(s1, s2);
            score += (int)Math.Round(cos * 1000.0 * WEIGHT_SKILL);

            return score;
        }

        private static HashSet<string> SafeWeaponClasses(WCharacter c)
        {
            try
            {
                return new HashSet<string>(
                    EquippedWeaponClasses(c).Where(s => !string.IsNullOrWhiteSpace(s)),
                    StringComparer.OrdinalIgnoreCase
                );
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static List<string> EquippedWeaponClasses(WCharacter c)
        {
            var classes = new List<string>();
            foreach (var slot in WEquipment.Slots)
            {
                var item = c.Equipment.GetItem(slot);
                if (item != null && item.IsWeapon)
                    classes.Add(item.Class);
            }
            return classes;
        }

        private static Dictionary<string, int> SafeSkills(WCharacter c)
        {
            try
            {
                var src = c?.Skills ?? [];
                var dict = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var kv in src)
                {
                    var skillObj = kv.Key;
                    var id = skillObj?.StringId;
                    if (string.IsNullOrEmpty(id))
                        continue;
                    dict[id] = kv.Value;
                }
                return dict;
            }
            catch
            {
                return new Dictionary<string, int>(StringComparer.Ordinal);
            }
        }
    }
}
