using System;
using Retinues.Configuration;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace OldRetinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for settlement notables, provides helpers for volunteer swapping and faction logic.
    /// </summary>
    [SafeClass]
    public class WNotable(Hero notable, WSettlement settlement) : WHero(notable)
    {
        // RNG for generating random floats
        private static readonly Random rng = new();

        private readonly WSettlement _settlement = settlement;
        public WSettlement Settlement => _settlement;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Swaps volunteers in the notable's array for the best matching troop from the given faction.
        /// Preserves empty slots and fixes corrupt entries.
        /// </summary>
        public void SwapVolunteers(WFaction faction)
        {
            SwapVolunteers(faction, null, 0f);
        }

        /// <summary>
        /// Swaps volunteers for the best matching troop from the primary faction, with an optional
        /// chance to use a secondary faction instead (for clan/kingdom mixing).
        /// </summary>
        public void SwapVolunteers(
            WFaction primaryFaction,
            WFaction secondaryFaction,
            float secondaryProportion
        )
        {
            if (Base == null || primaryFaction == null)
                return;

            var arr = Hero.VolunteerTypes;
            if (arr == null || arr.Length == 0)
                return;

            // Only treat secondary as active if both faction and a sensible proportion are provided.
            bool hasSecondary =
                secondaryFaction != null && secondaryProportion > 0f && secondaryProportion <= 1f;

            int replaced = 0;

            for (int i = 0; i < arr.Length; i++)
            {
                var vanilla = arr[i];

                // Preserve empty slots
                if (vanilla == null)
                    continue;

                var troop = new WCharacter(vanilla);

                if (!troop.IsValid)
                {
                    arr[i] = null; // Fix corrupt entries
                    continue;
                }

                // Already in one of our custom factions, skip.
                if (
                    troop.Faction == primaryFaction
                    || (hasSecondary && troop.Faction == secondaryFaction)
                )
                    continue;

                // Decide which faction to use for this slot.
                WFaction targetFaction = primaryFaction;
                if (hasSecondary && rng.NextDouble() < secondaryProportion)
                    targetFaction = secondaryFaction;

                if (targetFaction == null)
                    continue;

                var replacement = TroopMatcher.PickBestFromFaction(
                    targetFaction,
                    troop,
                    sameTierOnly: false
                );

                if (replacement != null)
                {
                    arr[i] = replacement.Base;
                    replaced++;
                }
            }

            if (replaced > 0)
            {
                var primaryId = primaryFaction?.StringId ?? "null";
                var secondaryId = hasSecondary ? secondaryFaction?.StringId : null;
                var mixInfo =
                    secondaryId != null
                        ? $" + mix {secondaryId} (p={secondaryProportion:0.##})"
                        : "";
                Log.Debug(
                    $"{Name} ({Settlement.Name}): swapped {replaced} volunteers to {primaryId}{mixInfo}."
                );
            }
        }

        /// <summary>
        /// Swaps a specific volunteer type in the notable's array.
        /// </summary>
        public void SwapVolunteer(WCharacter oldTroop, WCharacter newTroop)
        {
            if (Base == null || oldTroop == null || newTroop == null)
                return;

            var arr = Hero.VolunteerTypes;
            if (arr == null || arr.Length == 0)
                return;

            int replaced = 0;

            for (int i = 0; i < arr.Length; i++)
            {
                var vanilla = arr[i];

                // Preserve empty slots
                if (vanilla == null)
                    continue;

                var troop = new WCharacter(vanilla);
                if (troop == oldTroop)
                {
                    arr[i] = newTroop.Base;
                    replaced++;
                    break;
                }
            }

            if (replaced > 0)
                Log.Debug(
                    $"{Name} ({Settlement.Name}): swapped {replaced} {oldTroop.Name} ({oldTroop}) to {newTroop.Name} ({newTroop})."
                );
        }
    }
}
