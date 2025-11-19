using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;

namespace Retinues.Troops
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

        private static WCharacter Pick(WCharacter troop, BaseFaction faction)
        {
            if (troop == null || faction == null)
                return null;

            return troop switch
            {
                var _ when troop == troop.Faction.RetinueElite => faction.RetinueElite,
                var _ when troop == troop.Faction.RetinueBasic => faction.RetinueBasic,
                var _ when troop == troop.Faction.MilitiaMelee => faction.MilitiaMelee,
                var _ when troop == troop.Faction.MilitiaMeleeElite => faction.MilitiaMeleeElite,
                var _ when troop == troop.Faction.MilitiaRanged => faction.MilitiaRanged,
                var _ when troop == troop.Faction.MilitiaRangedElite => faction.MilitiaRangedElite,
                var _ when troop == troop.Faction.CaravanGuard => faction.CaravanGuard,
                var _ when troop == troop.Faction.CaravanMaster => faction.CaravanMaster,
                var _ when troop == troop.Faction.Villager => faction.Villager,
                _ => null,
            };
        }

        /// <summary>
        /// Pick the best matching troop from a faction's tree for the given troop.
        /// Special roles (villager/caravan/militia) use direct mapped slots; regular troops
        /// use similarity within the basic/elite tree, and only map to custom candidates.
        /// </summary>
        public static WCharacter PickBestFromFaction(
            BaseFaction faction,
            WCharacter troop,
            bool strict = false
        )
        {
            if (faction == null || troop == null || !troop.IsValid)
                return null;

            static bool Valid(WCharacter x) => x != null && x.IsValid && x.IsCustom;

            var srcFaction = troop.Faction;

            // 1. Slot detection
            bool isSlot =
                troop == srcFaction.Villager
                || troop == srcFaction.CaravanGuard
                || troop == srcFaction.CaravanMaster
                || troop == srcFaction.MilitiaMelee
                || troop == srcFaction.MilitiaMeleeElite
                || troop == srcFaction.MilitiaRanged
                || troop == srcFaction.MilitiaRangedElite
                || troop == srcFaction.RetinueBasic
                || troop == srcFaction.RetinueElite;

            // 2. Direct slot mapping
            if (isSlot)
            {
                var direct = Pick(troop, faction); // Maps to target slot
                if (Valid(direct))
                    return direct;

                // In strict mode: slot troops are allowed to fall through to tree match
                // If they are not in tree, they will be handled by strict logic below.
            }

            // 3. Detect tree membership
            bool isTreeMember =
                (srcFaction.RootBasic?.Tree?.Contains(troop) == true)
                || (srcFaction.RootElite?.Tree?.Contains(troop) == true);

            // 4. Strict mode restriction
            if (strict && !isSlot && !isTreeMember)
            {
                // Bandits, guards, etc.
                return null;
            }

            // 5. Tree matching
            if (isTreeMember)
            {
                var root = troop.IsElite ? faction.RootElite : faction.RootBasic;
                if (root != null && root.IsValid)
                {
                    var best = PickBestFromTree(root, troop);
                    if (Valid(best))
                        return best;
                }
            }

            // 6. Non-strict fallback
            if (!strict)
            {
                var reuseRoot = troop.IsElite ? faction.RootElite : faction.RootBasic;
                if (reuseRoot != null && reuseRoot.IsValid)
                {
                    var fallback = PickBestFromTree(reuseRoot, troop);
                    if (Valid(fallback))
                        return fallback;
                }
            }

            // 7. No match
            return null;
        }

        /// <summary>
        /// Pick the best matching troop from a tree for the given troop, optionally excluding one.
        /// Recomputes similarity every call (no caching).
        /// </summary>
        public static WCharacter PickBestFromTree(
            WCharacter root,
            WCharacter troop,
            WCharacter exclude = null
        )
        {
            if (root?.IsValid != true || troop == null || !troop.IsValid)
                return null;

            var candidates =
                root.Tree?.Where(t =>
                        t.IsValid
                        && t.Tier == troop.Tier
                        && (exclude == null || t.StringId != exclude.StringId)
                    )
                    .ToList() ?? [];

            if (candidates.Count == 0)
                return null;

            WCharacter best = null;
            int bestScore = 0;

            foreach (var candidate in candidates)
            {
                int score = EligibilityScore(candidate, troop);

                if (
                    best == null
                    || score > bestScore
                    || (
                        score == bestScore
                        && string.CompareOrdinal(candidate.StringId, best.StringId) < 0
                    )
                )
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best?.IsValid == true ? best : null;
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
            double jacc = Similarity.Jaccard(WeaponClasses(troop), WeaponClasses(retinue));
            score += (int)Math.Round(jacc * 1000.0 * WEIGHT_WEAP);

            // 5) Skillset similarity (cosine on shared keys)
            double cos = Similarity.Cosine(Skills(troop), Skills(retinue));
            score += (int)Math.Round(cos * 1000.0 * WEIGHT_SKILL);

            return score;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Weapon / Skill Extraction               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Get a set of weapon class strings for the equipped weapons of a troop.
        /// </summary>
        private static HashSet<string> WeaponClasses(WCharacter c)
        {
            try
            {
                var classes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (c == null)
                    return classes;

                foreach (var slot in WEquipment.Slots)
                {
                    try
                    {
                        var item = c.Loadout.Battle.Get(slot);
                        if (item != null && item.IsWeapon && !string.IsNullOrWhiteSpace(item.Class))
                            classes.Add(item.Class);
                    }
                    catch { }
                }

                return classes;
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Get a dictionary of skill string IDs to values for a troop.
        /// </summary>
        private static Dictionary<string, int> Skills(WCharacter c)
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
