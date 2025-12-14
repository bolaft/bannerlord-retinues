using System;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace OldRetinues.Safety.Sanitizer
{
    /// <summary>
    /// Utility for sanitizing settlement volunteers.
    /// Replaces or removes invalid volunteer troops for notables in settlements.
    /// </summary>
    [SafeClass]
    public static class VolunteerSanitizer
    {
        /// <summary>
        /// Cleans all notables' volunteers in a settlement, replacing invalid entries.
        /// </summary>
        public static void SanitizeSettlement(Settlement settlement, bool replaceAllCustom = false)
        {
            if (settlement == null)
                return;

            foreach (var notable in settlement.Notables)
            {
                if (notable == null)
                    continue;

                SanitizeNotable(notable, settlement, replaceAllCustom);
            }
        }

        /// <summary>
        /// Swaps or removes invalid volunteers for a notable in a settlement.
        /// </summary>
        private static void SanitizeNotable(
            Hero notable,
            Settlement settlement,
            bool replaceAllCustom = false
        )
        {
            if (settlement == null)
                return;

            if (notable?.VolunteerTypes == null)
                return;

            for (int i = 0; i < notable.VolunteerTypes.Length; i++)
            {
                try
                {
                    var c = notable.VolunteerTypes[i];

                    if (c == null)
                        continue;

                    if (SanitizerBehavior.IsCharacterValid(c, replaceAllCustom))
                        continue;

                    Log.Warn(
                        $"Invalid volunteer troop '{c.StringId}' found at notable '{notable.Name}' in settlement '{settlement.Name}'."
                    );

                    // Wrap the old troop
                    var troop = new WCharacter(c);

                    // Find fallback from the settlement's vanilla culture if possible
                    CharacterObject fallback = TroopMatcher
                        .PickBestFromFaction(new WSettlement(settlement).Culture, troop)
                        .Base;

                    if (fallback != null)
                    {
                        Log.Warn($"Fallback found, replacing with '{fallback}'");
                        notable.VolunteerTypes[i] = fallback;
                    }
                    else
                    {
                        Log.Warn("No fallback found, removing volunteer entry.");
                        notable.VolunteerTypes[i] = null;
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(
                        e,
                        $"Exception while processing notable '{notable.Name}' in settlement '{settlement.Name}'"
                    );
                }
            }
        }
    }
}
