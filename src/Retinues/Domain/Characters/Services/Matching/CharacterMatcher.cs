using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Base;
using Retinues.Domain.Factions.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Characters.Services.Matching
{
    public static class CharacterMatcher
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Picks the best matching character from the given collection based on the provided troop.
        /// </summary>
        public static WCharacter FindBest(
            WCharacter troop,
            IEnumerable<WCharacter> troops,
            bool strictTierMatch = true,
            bool strictCategoryMatch = true,
            WCharacter fallback = null,
            int? requestedTier = null
        )
        {
            if (troop == null || troops == null)
                return fallback;

            // Initial filtering by tier.
            if (strictTierMatch)
                troops = troops.Where(t => t.Tier == (requestedTier ?? troop.Tier));

            // Initial filtering by category.
            if (strictCategoryMatch)
            {
                troops = troops.Where(t =>
                    t.IsRegular == troop.IsRegular
                    && t.IsElite == troop.IsElite
                    && t.IsBasic == troop.IsBasic
                    && t.IsMercenary == troop.IsMercenary
                    && t.IsBandit == troop.IsBandit
                    && t.IsMilitia == troop.IsMilitia
                    && t.IsCaravan == troop.IsCaravan
                    && t.IsVillager == troop.IsVillager
                    && t.IsCivilian == troop.IsCivilian
                );
            }

            // Materialize candidates once and drop nulls.
            // Also de-dup by StringId to avoid bias from repeated entries.
            var candidates = new List<WCharacter>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var c in troops)
            {
                if (c == null)
                    continue;

                if (seen.Add(c.StringId))
                    candidates.Add(c);
            }

            if (candidates.Count == 0)
                return fallback;

            var tier = requestedTier ?? troop.Tier;

            // 1) Tier (closest, or strict exact)
            FilterByTier(tier, candidates);
            if (candidates.Count == 0)
                return fallback;
            if (candidates.Count == 1)
                return candidates[0];

            // 2) Mounted
            var desiredMounted = EffectiveIsMounted(troop);
            FilterByBoolMatch(desiredMounted, candidates, EffectiveIsMounted);
            if (candidates.Count == 1)
                return candidates[0];

            // 3) Ranged
            var desiredRanged = EffectiveIsRanged(troop);
            FilterByBoolMatch(desiredRanged, candidates, EffectiveIsRanged);
            if (candidates.Count == 1)
                return candidates[0];

            // 4) Weapon categories similarity
            FilterByWeaponCategoriesSimilarity(troop, candidates);
            if (candidates.Count == 1)
                return candidates[0];

            // 5) Skillset similarity
            FilterBySkillsSimilarity(troop, candidates);
            if (candidates.Count == 1)
                return candidates[0];

            return candidates.Count > 0 ? candidates[0] : fallback;
        }

        /// <summary>
        /// Finds the best matching counterpart of the given troop in the specified faction.
        /// </summary>
        public static WCharacter FindCounterpart(
            WCharacter troop,
            IBaseFaction faction,
            bool strictTierMatch = false,
            WCharacter fallback = null
        )
        {
            if (troop == null || faction == null)
                return fallback;

            if (troop.IsHero)
                return fallback;

            var roster = troop switch
            {
                { IsRegular: true } => troop.IsElite ? faction.RosterElite : faction.RosterBasic,
                { IsMercenary: true } => faction.RosterMercenary,
                { IsBandit: true } => faction.RosterBandit,
                _ => null,
            };

            // Troop is in a tree, find best match from that tree.
            if (roster != null)
                return FindBest(
                    troop,
                    roster,
                    strictTierMatch: strictTierMatch,
                    fallback: fallback
                );

            // Troop is not in a tree, try specific roles.
            // Prioritize by faction type: Clan > Kingdom > Culture.
            foreach (
                var f in troop.Factions.OrderBy(f =>
                    f switch
                    {
                        WClan _ => 0,
                        WKingdom _ => 1,
                        WCulture _ => 2,
                        _ => 3,
                    }
                )
            )
            {
                if (troop == f.Villager && faction.Villager != null && faction.Villager != troop)
                    return faction.Villager;

                if (
                    troop == f.MeleeMilitiaTroop
                    && faction.MeleeMilitiaTroop != null
                    && faction.MeleeMilitiaTroop != troop
                )
                    return faction.MeleeMilitiaTroop;

                if (
                    troop == f.MeleeEliteMilitiaTroop
                    && faction.MeleeEliteMilitiaTroop != null
                    && faction.MeleeEliteMilitiaTroop != troop
                )
                    return faction.MeleeEliteMilitiaTroop;

                if (
                    troop == f.RangedMilitiaTroop
                    && faction.RangedMilitiaTroop != null
                    && faction.RangedMilitiaTroop != troop
                )
                    return faction.RangedMilitiaTroop;

                if (
                    troop == f.RangedEliteMilitiaTroop
                    && faction.RangedEliteMilitiaTroop != null
                    && faction.RangedEliteMilitiaTroop != troop
                )
                    return faction.RangedEliteMilitiaTroop;

                if (
                    troop == f.CaravanGuard
                    && faction.CaravanGuard != null
                    && faction.CaravanGuard != troop
                )
                    return faction.CaravanGuard;

                if (
                    troop == f.CaravanMaster
                    && faction.CaravanMaster != null
                    && faction.CaravanMaster != troop
                )
                    return faction.CaravanMaster;

                if (
                    troop == f.ArmedTrader
                    && faction.ArmedTrader != null
                    && faction.ArmedTrader != troop
                )
                    return faction.ArmedTrader;
            }

            return fallback;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Tier Match                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Filters candidates by tier, keeping those closest to the target tier.
        /// </summary>
        private static void FilterByTier(int tier, List<WCharacter> candidates)
        {
            int bestDist = int.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                var dist = Math.Abs(c.Tier - tier);
                if (dist < bestDist)
                    bestDist = dist;
            }

            // Keep only those with best distance.
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                var c = candidates[i];
                if (Math.Abs(c.Tier - tier) != bestDist)
                    candidates.RemoveAt(i);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Bool Match                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Filters candidates by a boolean selector, keeping those that match the desired value.
        /// </summary>
        private static void FilterByBoolMatch(
            bool desired,
            List<WCharacter> candidates,
            Func<WCharacter, bool> selector
        )
        {
            // Only filter if at least one candidate matches.
            bool any = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (selector(candidates[i]) == desired)
                {
                    any = true;
                    break;
                }
            }

            if (!any)
                return;

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (selector(candidates[i]) != desired)
                    candidates.RemoveAt(i);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //              Weapon Categories Similarity              //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Filters candidates by weapon categories similarity to the given troop.
        /// </summary>
        private static void FilterByWeaponCategoriesSimilarity(
            WCharacter troop,
            List<WCharacter> candidates
        )
        {
            if (candidates == null || candidates.Count <= 1)
                return;

            var troopWeapons = new HashSet<WeaponClass>();
            CollectWeaponClasses(troop, troopWeapons);

            // If we cannot compute a meaningful profile, keep the current set.
            if (troopWeapons.Count == 0)
                return;

            var tmp = new HashSet<WeaponClass>();

            int bestScore = int.MinValue;
            var scores = new int[candidates.Count];

            for (int i = 0; i < candidates.Count; i++)
            {
                tmp.Clear();
                CollectWeaponClasses(candidates[i], tmp);

                var score = JaccardScoreThousand(troopWeapons, tmp);
                scores[i] = score;
                if (score > bestScore)
                    bestScore = score;
            }

            // Keep best score only.
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (scores[i] != bestScore)
                    candidates.RemoveAt(i);
            }
        }

        /// <summary>
        /// Collects the weapon classes used by the given wrapped character.
        /// </summary>
        private static void CollectWeaponClasses(WCharacter wc, HashSet<WeaponClass> set)
        {
            if (wc == null || set == null)
                return;

            var eq = wc.FirstBattleEquipment;
            if (eq == null)
                return;

            foreach (var item in eq.Items)
            {
                if (item == null)
                    continue;

                // Only count weapon-like items that expose a PrimaryWeapon.
                // This includes melee, ranged, shields, ammo.
                if (!item.IsWeapon && !item.IsShield && !item.IsAmmo)
                    continue;

                var w = item.PrimaryWeapon;
                if (w == null)
                    continue;

                set.Add(w.WeaponClass);
            }
        }

        /// <summary>
        /// Computes the Jaccard similarity score (0-1000) between two sets of weapon classes.
        /// </summary>
        private static int JaccardScoreThousand(HashSet<WeaponClass> a, HashSet<WeaponClass> b)
        {
            if (a == null || b == null)
                return 0;

            int aCount = a.Count;
            int bCount = b.Count;
            if (aCount == 0 && bCount == 0)
                return 0;

            int intersection = 0;

            // Iterate smaller set for intersection.
            if (aCount <= bCount)
            {
                foreach (var x in a)
                    if (b.Contains(x))
                        intersection++;
            }
            else
            {
                foreach (var x in b)
                    if (a.Contains(x))
                        intersection++;
            }

            int union = aCount + bCount - intersection;
            if (union <= 0)
                return 0;

            return intersection * 1000 / union;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Skillset Similarity                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Filters candidates by skillset similarity to the given troop.
        /// </summary>
        private static void FilterBySkillsSimilarity(WCharacter troop, List<WCharacter> candidates)
        {
            if (troop == null || candidates == null || candidates.Count <= 1)
                return;

            var skills = troop.Skills.ToList();
            if (skills == null || skills.Count == 0)
                return;

            var target = skills.Select(s => troop.Skills.Get(s.Skill)).ToArray();

            // Compute the sum of absolute differences for each candidate
            var scores = candidates
                .Select(c =>
                    skills.Select((s, i) => Math.Abs(c.Skills.Get(s.Skill) - target[i])).Sum()
                )
                .ToList();

            int best = scores.Min();

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (scores[i] != best)
                    candidates.RemoveAt(i);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Mounted / Ranged                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines if the given wrapped character is effectively mounted.
        /// </summary>
        private static bool EffectiveIsMounted(WCharacter wc)
        {
            if (wc == null)
                return false;

            var eq = wc.FirstBattleEquipment;
            if (eq != null)
                return eq.FormationInfo.IsMounted;

            return wc.IsMounted;
        }

        /// <summary>
        /// Determines if the given wrapped character is effectively ranged.
        /// </summary>
        private static bool EffectiveIsRanged(WCharacter wc)
        {
            if (wc == null)
                return false;

            var eq = wc.FirstBattleEquipment;
            if (eq != null)
                return eq.FormationInfo.IsRanged;

            return wc.IsRanged;
        }
    }
}
