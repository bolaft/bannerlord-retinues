using System;
using System.Collections.Generic;
using System.Linq;
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
        //                         Caches                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly Dictionary<string, Dictionary<string, string>> _retinueMatchCache =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Dictionary<string, string>> _regularMatchCache =
            new(StringComparer.Ordinal); // key = regularId, value = (rootId -> customId)

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Matching                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Pick the best matching troop from a faction's tree for the given troop.
        /// </summary>
        public static WCharacter PickBestFromFaction(
            WFaction faction,
            WCharacter troop,
            HashSet<string> troopWeapons = null,
            Dictionary<string, int> troopSkills = null
        )
        {
            if (faction == null || troop == null || !troop.IsValid)
                return null;

            // 1) SPECIAL ROLES: villager / caravan
            // Only map if the faction slot exists AND is a custom troop.
            if (troop.IsVillager)
            {
                var v = faction.Villager;
                return (v != null && v.IsValid && v.IsCustom) ? v : null;
            }
            if (troop.IsCaravanMaster)
            {
                var cm = faction.CaravanMaster;
                return (cm != null && cm.IsValid && cm.IsCustom) ? cm : null;
            }
            if (troop.IsCaravanGuard)
            {
                var cg = faction.CaravanGuard;
                return (cg != null && cg.IsValid && cg.IsCustom) ? cg : null;
            }

            // 2) MILITIA: keep melee/ranged and basic/elite strictly aligned
            if (troop.IsMilitia)
            {
                WCharacter pick = null;
                if (troop.IsElite)
                    pick = troop.IsMilitiaRanged
                        ? faction.MilitiaRangedElite
                        : faction.MilitiaMeleeElite;
                else
                    pick = troop.IsMilitiaRanged ? faction.MilitiaRanged : faction.MilitiaMelee;

                return (pick != null && pick.IsValid && pick.IsCustom) ? pick : null;
            }

            // 3) REGULAR BASIC/ELITE: same tree (basic vs elite) and tier,
            //    but accept ONLY a custom candidate; otherwise keep the original.
            var root = troop.IsElite ? faction.RootElite : faction.RootBasic;
            if (root == null || !root.IsValid)
                return null;

            if (
                TryGetCachedMatch(root, troop, excludeId: null, out var cached)
                && cached?.IsCustom == true
            )
                return cached;

            troopWeapons ??= SafeWeaponClasses(troop);
            troopSkills ??= SafeSkills(troop);

            var best = PickBestFromTree(
                root,
                troop,
                troopWeapons: troopWeapons,
                troopSkills: troopSkills
            );

            // STRICT: only map to custom; otherwise no-op at caller via ?? e.Troop
            return (best != null && best.IsValid && best.IsCustom) ? best : null;
        }

        /// <summary>
        /// Pick the best matching troop from a tree for the given troop, optionally excluding one.
        /// </summary>
        public static WCharacter PickBestFromTree(
            WCharacter root,
            WCharacter troop,
            WCharacter exclude = null,
            HashSet<string> troopWeapons = null,
            Dictionary<string, int> troopSkills = null
        )
        {
            if (root?.IsValid != true)
                return null;

            if (TryGetCachedMatch(root, troop, exclude?.StringId, out var cachedMatch))
            {
                return cachedMatch;
            }

            var candidates =
                root.Tree?.Where(t =>
                        t.IsValid
                        && t.Tier == troop.Tier
                        && (exclude == null || t.StringId != exclude.StringId)
                    )
                    .ToList() ?? [];

            if (candidates.Count == 0)
                return null;

            troopWeapons ??= SafeWeaponClasses(troop);
            troopSkills ??= SafeSkills(troop);

            WCharacter best = null;
            int bestScore = 0;

            foreach (var candidate in candidates)
            {
                int score = EligibilityScore(candidate, troop, troopWeapons, troopSkills);

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

            if (best?.IsValid == true)
            {
                var bestId = best.StringId;
                var troopId = troop.StringId;

                if (string.IsNullOrEmpty(bestId) || string.IsNullOrEmpty(troopId))
                    return best;

                if (troop.IsRetinue)
                    CacheRetinueMatch(troopId, root, bestId);
                else if (!troop.IsCustom && best.IsCustom)
                    CacheRegularMatch(root, troopId, bestId);
            }

            return best;
        }

        /// <summary>
        /// Compute a weighted eligibility score between two troops.
        /// </summary>
        private static int EligibilityScore(
            WCharacter troop,
            WCharacter retinue,
            HashSet<string> troopWeapons,
            Dictionary<string, int> troopSkills
        )
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
            double jacc = Similarity.Jaccard(w1, troopWeapons);
            score += (int)Math.Round(jacc * 1000.0 * WEIGHT_WEAP);

            // 5) Skillset similarity (cosine on shared keys)
            var s1 = SafeSkills(troop);
            double cos = Similarity.Cosine(s1, troopSkills);
            score += (int)Math.Round(cos * 1000.0 * WEIGHT_SKILL);

            return score;
        }

        /// <summary>
        ///  Get weapon classes and skills for a troop.
        /// </summary>
        public static (
            HashSet<string> Weapons,
            Dictionary<string, int> Skills
        ) GetTroopClassesSkills(WCharacter troop)
        {
            if (troop == null)
                return (
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, int>(StringComparer.Ordinal)
                );

            return (SafeWeaponClasses(troop), SafeSkills(troop));
        }

        /// <summary>
        /// Invalidate cached matches for a given troop.
        /// </summary>
        public static void InvalidateTroopCache(WCharacter troop)
        {
            var id = troop?.StringId;
            if (string.IsNullOrEmpty(id))
                return;

            if (troop.IsRetinue)
            {
                // Invalidate both slots for retinues
                _retinueMatchCache.Remove(id);
            }
            else
            {
                // Invalidate entire regular cache because custom troops can change into becoming better matches instead of the existing cache entry
                _regularMatchCache.Clear();
            }
        }

        /// <summary>
        /// Try to get a cached troop match for the given root and troop.
        /// </summary>
        private static bool TryGetCachedMatch(
            WCharacter root,
            WCharacter troop,
            string excludeId,
            out WCharacter cached
        )
        {
            cached = null;
            if (troop == null)
                return false;

            var troopId = troop.StringId;
            if (string.IsNullOrEmpty(troopId))
                return false;

            if (troop.IsRetinue)
                return TryGetCachedRetinueMatch(root, troopId, excludeId, out cached);

            if (!troop.IsCustom)
                return TryGetCachedRegularMatch(root, troopId, excludeId, out cached);

            return false;
        }

        private static void CacheRetinueMatch(string retId, WCharacter root, string matchId)
        {
            if (root?.IsValid != true)
                return;
            var rootId = root.StringId;
            if (string.IsNullOrEmpty(rootId) || string.IsNullOrEmpty(matchId))
                return;

            if (!_retinueMatchCache.TryGetValue(retId, out var map) || map == null)
            {
                map = new Dictionary<string, string>(StringComparer.Ordinal);
                _retinueMatchCache[retId] = map;
            }

            map[rootId] = matchId;
        }

        private static bool TryGetCachedRetinueMatch(
            WCharacter root,
            string retId,
            string excludeId,
            out WCharacter cached
        )
        {
            cached = null;
            if (root?.IsValid != true)
                return false;

            if (!_retinueMatchCache.TryGetValue(retId, out var map) || map == null)
                return false;

            var rootId = root.StringId;
            if (string.IsNullOrEmpty(rootId) || !map.TryGetValue(rootId, out var matchId))
                return false;

            if (string.IsNullOrEmpty(matchId) || matchId == excludeId)
                return false;

            cached = CreateSafe(matchId);
            if (cached?.IsValid != true)
            {
                map.Remove(rootId);
                cached = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Cache a regular troop match. Caches only non-custom to custom matches.
        /// </summary>
        private static void CacheRegularMatch(WCharacter root, string regularId, string matchId)
        {
            var rootId = root?.StringId;
            if (
                string.IsNullOrEmpty(rootId)
                || string.IsNullOrEmpty(regularId)
                || string.IsNullOrEmpty(matchId)
            )
                return;

            if (!_regularMatchCache.TryGetValue(regularId, out var map) || map == null)
            {
                map = new Dictionary<string, string>(StringComparer.Ordinal);
                _regularMatchCache[regularId] = map;
            }
            map[rootId] = matchId;
        }

        /// <summary>
        /// Try to get a cached regular troop match.
        /// </summary>
        private static bool TryGetCachedRegularMatch(
            WCharacter root,
            string regularId,
            string excludeId,
            out WCharacter cached
        )
        {
            cached = null;
            var rootId = root?.StringId;
            if (string.IsNullOrEmpty(rootId) || string.IsNullOrEmpty(regularId))
                return false;

            if (!_regularMatchCache.TryGetValue(regularId, out var map) || map == null)
                return false;

            if (
                !map.TryGetValue(rootId, out var matchId)
                || string.IsNullOrEmpty(matchId)
                || matchId == excludeId
            )
                return false;

            cached = CreateSafe(matchId);
            if (cached?.IsValid != true)
            {
                map.Remove(rootId);
                cached = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create a WCharacter safely from an ID, returning null if invalid.
        /// </summary>
        private static WCharacter CreateSafe(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            try
            {
                var troop = new WCharacter(id);
                return troop.IsValid ? troop : null;
            }
            catch
            {
                return null;
            }
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
                var item = c.Loadout.Battle.Get(slot);
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
