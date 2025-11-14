using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.ObjectSystem;

namespace Retinues.Safety.Sanitizer
{
    /// <summary>
    /// Utility for sanitizing party and troop rosters.
    /// Cleans up invalid, null, or corrupted troop entries and replaces them with safe fallbacks.
    /// </summary>
    [SafeClass]
    public static class PartySanitizer
    {
        /// <summary>
        /// Sanitizes both member and prison rosters of a MobileParty.
        /// </summary>
        public static void SanitizeParty(MobileParty mp, bool replaceAllCustom = false)
        {
            if (mp == null)
                return;

            SanitizeRoster(mp.MemberRoster, mp, replaceAllCustom);
            SanitizeRoster(mp.PrisonRoster, mp, replaceAllCustom);
        }

        /// <summary>
        /// Sanitizes a TroopRoster, replacing invalid or null entries and normalizing counts.
        /// </summary>
        public static void SanitizeRoster(
            TroopRoster roster,
            MobileParty party = null,
            bool replaceAllCustom = false
        )
        {
            if (roster == null)
                return;

            var replacements = new List<Action>();

            try
            {
                for (int i = roster.Count - 1; i >= 0; i--)
                {
                    var e = roster.GetElementCopyAtIndex(i);

                    // Need to replace invalid or null CharacterObjects
                    if (!SanitizerBehavior.IsCharacterValid(e.Character, replaceAllCustom))
                    {
                        Log.Warn(
                            $"Invalid troop '{e.Character?.StringId ?? "NULL"}' found at index {i} in {party?.Name?.ToString() ?? "unknown party"}."
                        );
                        replacements.Add(() => ReplaceInvalidTroop(roster, e));
                    }
                }

                // Execute all replacement actions after iteration
                foreach (var replace in replacements)
                    replace();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Failed while cleaning roster");
            }
        }

        /// <summary>
        /// Replaces a roster element with a fallback troop or removes it if no fallback is available.
        /// </summary>
        private static void ReplaceInvalidTroop(TroopRoster roster, TroopRosterElement element)
        {
            if (element.Character == null)
                return; // Nothing to replace

            CharacterObject fallback = null;

            try
            {
                // Wrap the old troop
                WCharacter troop = new(element.Character);

                // Find fallback from the troop's vanilla culture if possible
                fallback = TroopMatcher.PickBestFromFaction(troop.Culture, troop).Base;
            }
            catch (Exception) { }

            // If no fallback found, try a generic one
            fallback ??= MBObjectManager.Instance?.GetObject<CharacterObject>("looter");

            // Add new troop at same index
            roster.AddToCounts(
                fallback,
                element.Number,
                woundedCount: element.WoundedNumber,
                xpChange: element.Xp,
                index: roster.FindIndexOfTroop(element.Character)
            );

            // Remove old troop
            roster.AddToCounts(
                element.Character,
                -element.Number,
                woundedCount: -element.WoundedNumber
            );

            Log.Info(
                $"Replaced '{element.Character.StringId}' with '{fallback.StringId}' (count: {element.Number}, wounded: {element.WoundedNumber})."
            );
        }
    }
}
