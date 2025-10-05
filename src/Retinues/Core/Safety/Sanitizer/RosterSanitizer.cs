using System;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.ObjectSystem;

namespace Retinues.Core.Safety.Sanitizer
{
    /// <summary>
    /// Utility for sanitizing party and troop rosters.
    /// Cleans up invalid, null, or corrupted troop entries and replaces them with safe fallbacks.
    /// </summary>
    [SafeClass]
    public static class RosterSanitizer
    {
        /// <summary>
        /// Cleans both member and prison rosters of a MobileParty.
        /// </summary>
        public static void CleanParty(MobileParty mp)
        {
            if (mp == null)
                return;

            CleanRoster(mp.MemberRoster, mp);
            CleanRoster(mp.PrisonRoster, mp);
        }

        /// <summary>
        /// Cleans a TroopRoster, replacing invalid or null entries and normalizing counts.
        /// </summary>
        public static void CleanRoster(TroopRoster roster, MobileParty contextParty = null)
        {
            if (roster == null)
                return;

            try
            {
                for (int i = roster.Count - 1; i >= 0; i--)
                {
                    var e = roster.GetElementCopyAtIndex(i);

                    // Null character
                    if (e.Character == null)
                    {
                        Log.Warn(
                            $"Null troop at index {i} in {DescribeParty(contextParty)} - replacing."
                        );
                        ReplaceElementWithFallback(roster, i, e, contextParty);
                        continue;
                    }

                    // Validate
                    if (!IsCharacterValid(e.Character))
                    {
                        Log.Warn(
                            $"Invalid troop '{e.Character?.StringId ?? "NULL"}' at index {i} in {DescribeParty(contextParty)} - replacing."
                        );
                        ReplaceElementWithFallback(roster, i, e, contextParty);
                        continue;
                    }

                    // Clamp weird counts to avoid TW index math surprises later.
                    int total = Math.Max(0, e.Number);
                    int wounded = Math.Max(0, Math.Min(e.WoundedNumber, total));
                    int xp = Math.Max(0, e.Xp);

                    if (total != e.Number || wounded != e.WoundedNumber || xp != e.Xp)
                    {
                        // Normalize counts by removing the old entry and re-adding with clamped values.
                        // Do "replace with itself" to keep the same CharacterObject.
                        SafeReplace(roster, i, e.Character, total, wounded, xp);
                        Log.Info(
                            $"Normalized counts for '{e.Character.StringId}' at index {i} "
                                + $"(total:{e.Number}->{total}, wounded:{e.WoundedNumber}->{wounded}, xp:{e.Xp}->{xp})"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Failed while cleaning roster");
            }
        }

        /// <summary>
        /// Replaces a roster element with a fallback troop or removes it if no fallback is available.
        /// </summary>
        private static void ReplaceElementWithFallback(
            TroopRoster roster,
            int index,
            TroopRosterElement elem,
            MobileParty contextParty
        )
        {
            int total = Math.Max(0, elem.Number);
            if (total == 0)
            {
                if (elem.Character != null)
                    roster.RemoveTroop(elem.Character, 0); // no-op safety
                return;
            }

            var fallback = GetFallbackTroop(contextParty);
            fallback ??= MBObjectManager.Instance?.GetObject<CharacterObject>("looter"); // always present fallback
            if (fallback == null)
            {
                // Last resort: remove the bad entry entirely.
                if (elem.Character != null)
                    roster.RemoveTroop(elem.Character, total);
                Log.Warn(
                    $"No fallback troop; removed '{elem.Character?.StringId ?? "NULL"}' x{total}."
                );
                return;
            }

            // 1) Add the fallback (no wounds/xp)
            roster.AddToCounts(
                fallback,
                total,
                insertAtFront: false,
                woundedCount: 0,
                xpChange: 0,
                removeDepleted: true,
                index: -1
            );

            // 2) Remove the bad ones via the public helper (lets TW do the indexing safely)
            if (elem.Character != null)
                roster.RemoveTroop(elem.Character, total);

            Log.Info(
                $"Replaced '{elem.Character?.StringId ?? "NULL"}' with '{fallback.StringId}' (count:{total})."
            );
        }

        /// <summary>
        /// Safely replaces a roster element with new values, normalizing counts.
        /// </summary>
        private static void SafeReplace(
            TroopRoster roster,
            int index,
            CharacterObject newChar,
            int total,
            int wounded,
            int xp
        )
        {
            // Remove whatever is currently there.
            var old = roster.GetElementCopyAtIndex(index);
            if (old.Character != null && (old.Number != 0 || old.WoundedNumber != 0 || old.Xp != 0))
            {
                roster.AddToCounts(
                    old.Character,
                    count: -(old.Number),
                    insertAtFront: false,
                    woundedCount: -(old.WoundedNumber),
                    xpChange: -(old.Xp),
                    removeDepleted: true,
                    index: index
                );
            }

            // Insert replacement if requested
            if (newChar != null && total > 0)
            {
                roster.AddToCounts(
                    newChar,
                    count: total,
                    insertAtFront: false,
                    woundedCount: wounded,
                    xpChange: xp,
                    removeDepleted: true,
                    index: index
                );
            }
        }

        /// <summary>
        /// Gets a fallback troop for the context party, preferring culture basic troop or "looter".
        /// </summary>
        private static CharacterObject GetFallbackTroop(MobileParty contextParty)
        {
            // Prefer the context party's culture basic troop if we can reach it
            CharacterObject pick = null;

            var culture =
                contextParty?.MapFaction?.Culture ?? contextParty?.HomeSettlement?.Culture;

            try
            {
                pick = culture?.BasicTroop;
            }
            catch { }

            // Otherwise try a safe, always-present fallback
            pick ??= MBObjectManager.Instance?.GetObject<CharacterObject>("looter");

            // Final sanity: ensure it's a valid, active CharacterObject
            return IsCharacterValid(pick) ? pick : null;
        }

        /// <summary>
        /// Checks if a CharacterObject is valid and active in the object manager.
        /// </summary>
        private static bool IsCharacterValid(CharacterObject c)
        {
            if (c == null)
                return false;

            // Wrapper knows how to detect inactive/unregistered TW objects.
            var w = new WCharacter(c);
            if (!w.IsValid)
                return false;

            // Ensure the object manager can resolve it back
            var fromDb = MBObjectManager.Instance?.GetObject<CharacterObject>(c.StringId);
            if (!ReferenceEquals(fromDb, c) && fromDb == null)
                return false;

            return true;
        }

        /// <summary>
        /// Returns a string description of the party for logging.
        /// </summary>
        private static string DescribeParty(MobileParty mp)
        {
            if (mp == null)
                return "unknown party";
            try
            {
                return $"party '{mp?.Name?.ToString() ?? "?"}' [{mp?.Party?.Id?.ToString() ?? "no-id"}]";
            }
            catch
            {
                return "party (name/id unavailable)";
            }
        }
    }
}
