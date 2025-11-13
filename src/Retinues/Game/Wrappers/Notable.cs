using System;
using Retinues.Configuration;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for settlement notables, provides helpers for volunteer swapping and faction logic.
    /// </summary>
    [SafeClass]
    public class WNotable(Hero notable) : WHero(notable)
    {
        // RNG for generating random floats
        private static readonly Random rng = new();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Swaps volunteers in the notable's array for the best matching troop from the given faction.
        /// Preserves empty slots and fixes corrupt entries.
        /// </summary>
        public void SwapVolunteers(WFaction faction)
        {
            if (Base == null || faction == null)
                return;

            var arr = Base.VolunteerTypes;
            if (arr == null || arr.Length == 0)
                return;

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

                if (troop.Faction == faction)
                    continue; // Already correct faction troop

                if (rng.NextDouble() > Config.VolunteerSwapProportion)
                    continue; // Skip some based on proportion

                var replacement = TroopMatcher.PickBestFromFaction(faction, troop);

                Log.Info(
                    $"{Name}: replacing volunteer {troop.Name} with {replacement?.Name} from faction {faction.StringId}"
                );

                if (replacement != null)
                    arr[i] = replacement.Base;
            }
        }

        /// <summary>
        /// Swaps a specific volunteer type in the notable's array.
        /// </summary>
        public void SwapVolunteer(WCharacter oldTroop, WCharacter newTroop)
        {
            if (Base == null || oldTroop == null || newTroop == null)
                return;

            var arr = Base.VolunteerTypes;
            if (arr == null || arr.Length == 0)
                return;

            for (int i = 0; i < arr.Length; i++)
            {
                var vanilla = arr[i];

                // Preserve empty slots
                if (vanilla == null)
                    continue;

                var troop = new WCharacter(vanilla);
                if (troop == oldTroop)
                {
                    Log.Debug($"{Name}: swapping volunteer {oldTroop.Name} to {newTroop.Name}.");
                    arr[i] = newTroop.Base;
                    break;
                }
            }
        }
    }
}
