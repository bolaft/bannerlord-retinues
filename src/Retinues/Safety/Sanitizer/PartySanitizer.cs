using System;
using System.Collections.Generic;
using System.Reflection;
using Retinues.Game;
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
            try
            {
                // Case 1: Character is null -> remove corrupted entries
                if (element.Character == null)
                {
                    RemoveNullEntries(roster);
                    return;
                }

                // Case 2: Non-null but invalid character -> existing fallback logic
                CharacterObject fallback = null;

                try
                {
                    var troop = new WCharacter(element.Character);
                    fallback = TroopMatcher.PickBestFromFaction(troop.Culture, troop).Base;
                }
                catch
                {
                    // ignore, fallback below
                }

                fallback ??= Player.Culture?.RootBasic?.Base;
                fallback ??= MBObjectManager.Instance?.GetObject<CharacterObject>("looter");

                if (fallback == null)
                {
                    roster.RemoveIf(e => e.Character == element.Character);

                    Log.Warn(
                        $"Could not find fallback for invalid troop "
                            + $"'{element.Character?.StringId ?? "NULL"}'; removed from roster."
                    );
                    return;
                }

                var index = roster.FindIndexOfTroop(element.Character);

                roster.AddToCounts(
                    fallback,
                    element.Number,
                    woundedCount: element.WoundedNumber,
                    xpChange: element.Xp,
                    index: index >= 0 ? index : -1
                );

                roster.AddToCounts(
                    element.Character,
                    -element.Number,
                    woundedCount: -element.WoundedNumber
                );

                Log.Info(
                    $"Replaced '{element.Character.StringId}' with '{fallback.StringId}' "
                        + $"(count: {element.Number}, wounded: {element.WoundedNumber})."
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "ReplaceInvalidTroop failed while sanitizing roster");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Null cleanup                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        private static readonly FieldInfo TroopRosterDataField = typeof(TroopRoster).GetField(
            "data",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        private static readonly FieldInfo TroopRosterCountField = typeof(TroopRoster).GetField(
            "_count",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        /// <summary>
        /// Removes all entries with Character == null directly from the roster's internal array.
        /// Bypasses RemoveIf/AddToCountsAtIndex to avoid WoundedNumber/IsHero NREs.
        /// </summary>
        private static void RemoveNullEntries(TroopRoster roster)
        {
            try
            {
                if (roster == null)
                    return;

                if (TroopRosterDataField == null || TroopRosterCountField == null)
                {
                    Log.Warn(
                        "RemoveNullEntries: could not reflect TroopRoster.data/_count; aborting."
                    );
                    return;
                }

                var data = (TroopRosterElement[])TroopRosterDataField.GetValue(roster);
                var count = (int)TroopRosterCountField.GetValue(roster);

                if (data == null || count <= 0)
                    return;

                int dst = 0;
                int removed = 0;

                // Compact non-null entries to the front
                for (int src = 0; src < count; src++)
                {
                    if (data[src].Character == null)
                    {
                        removed++;
                        continue;
                    }

                    if (dst != src)
                        data[dst] = data[src];

                    dst++;
                }

                // Clear the tail with default struct values
                for (int i = dst; i < count; i++)
                {
                    data[i] = default; // Character=null, numbers=0, xp=0, etc.
                }

                if (removed > 0)
                {
                    TroopRosterCountField.SetValue(roster, dst);
                    roster.UpdateVersion(); // public method, no reflection needed

                    Log.Info($"Removed {removed} NULL troop roster element(s) from roster.");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "RemoveNullEntries failed while sanitizing roster");
            }
        }
    }
}
