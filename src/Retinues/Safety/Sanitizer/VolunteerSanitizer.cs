using System;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace Retinues.Safety.Sanitizer
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
        public static void CleanSettlement(Settlement settlement, bool replaceAllCustom = false)
        {
            if (settlement == null)
                return;

            foreach (var notable in settlement.Notables)
            {
                if (notable == null)
                    continue;

                SwapNotable(notable, settlement, replaceAllCustom);
            }
        }

        /// <summary>
        /// Swaps or removes invalid volunteers for a notable in a settlement.
        /// </summary>
        private static void SwapNotable(
            Hero notable,
            Settlement settlement,
            bool replaceAllCustom = false
        )
        {
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

                    var fallback = GetFallbackVolunteer(settlement);
                    if (fallback != null)
                    {
                        Log.Warn(
                            $"Replacing invalid volunteer at [{settlement?.Name}] "
                                + $"notable '{notable?.Name}' slot {i} "
                                + $"('{c?.StringId ?? "NULL"}' -> '{fallback.StringId}')."
                        );
                        notable.VolunteerTypes[i] = fallback;
                    }
                    else
                    {
                        Log.Warn(
                            $"Removing invalid volunteer at [{settlement?.Name}] "
                                + $"notable '{notable?.Name}' slot {i} ('{c?.StringId ?? "NULL"}')."
                        );
                        notable.VolunteerTypes[i] = null;
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(
                        e,
                        $"Exception while processing notable '{notable?.Name}' in settlement '{settlement?.Name}'"
                    );
                }
            }
        }

        /// <summary>
        /// Gets a fallback volunteer for a settlement, preferring culture basic troop or "looter".
        /// </summary>
        private static CharacterObject GetFallbackVolunteer(Settlement settlement)
        {
            CharacterObject pick = null;

            try
            {
                pick = settlement?.Culture?.BasicTroop;
            }
            catch { }

            pick ??= MBObjectManager.Instance?.GetObject<CharacterObject>("looter");

            return SanitizerBehavior.IsCharacterValid(pick) ? pick : null;
        }
    }
}
