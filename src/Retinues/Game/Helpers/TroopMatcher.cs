using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Utils;

namespace Retinues.Game.Helpers
{
    /// <summary>
    /// Static helpers for picking the best matching troop from a faction or tree.
    /// Uses weighted similarity on mount, ranged, gender, weapons, and skills.
    /// </summary>
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

        /// <summary>
        /// Pick the best matching troop from a faction's tree for the given troop.
        /// </summary>
        public static WCharacter PickBestFromFaction(WFaction faction, WCharacter troop)
        {
            if (faction == null || troop == null || !troop.IsValid)
                return null;

            var root = troop.IsElite ? faction.RootElite : faction.RootBasic;
            if (root == null || !root.IsValid)
                return null;

            return PickBestFromTree(root, troop);
        }

        /// <summary>
        /// Pick the best matching militia troop from a faction for the given troop.
        /// </summary>
        public static WCharacter PickMilitiaFromFaction(WFaction faction, WCharacter troop)
        {
            if (faction == null || troop == null || !troop.IsValid)
                return null;

            WCharacter pick;

            if (troop.IsElite)
                pick = troop.IsRanged ? faction.MilitiaRangedElite : faction.MilitiaMeleeElite;
            else
                pick = troop.IsRanged ? faction.MilitiaRanged : faction.MilitiaMelee;

            if (pick == null || !pick.IsValid)
                return null;

            return pick;
        }

        /// <summary>
        /// Pick the best matching troop from a tree for the given troop, optionally excluding one.
        /// </summary>
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

        /// <summary>
        /// Compute a weighted eligibility score between two troops.
        /// </summary>
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

        /// <summary>
        /// Get a set of weapon class strings for the equipped weapons of a troop.
        /// </summary>
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

        /// <summary>
        /// Get a list of weapon class strings for all equipped weapons of a troop.
        /// </summary>
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

        /// <summary>
        /// Get a dictionary of skill string IDs to values for a troop.
        /// </summary>
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
