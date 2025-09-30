using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.ObjectSystem;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Safety
{
    public static class RosterSanitizer
    {
        public static void CleanParty(MobileParty mp)
        {
            if (mp == null) return;
            CleanRoster(mp.MemberRoster, mp);
            CleanRoster(mp.PrisonRoster, mp);
        }

        public static void CleanRoster(TroopRoster roster, MobileParty contextParty = null)
        {
            if (roster == null) return;

            try
            {
                for (int i = roster.Count - 1; i >= 0; i--)
                {
                    var elem = roster.GetElementCopyAtIndex(i);

                    // Null character
                    if (elem.Character == null)
                    {
                        Log.Warn($"[RosterSanitizer] Null troop at index {i} in {DescribeParty(contextParty)} – replacing.");
                        ReplaceElementWithFallback(roster, i, elem, contextParty);
                        continue;
                    }

                    // Validate
                    if (!IsCharacterValid(elem.Character))
                    {
                        Log.Warn($"[RosterSanitizer] Invalid troop '{elem.Character?.StringId ?? "NULL"}' at index {i} in {DescribeParty(contextParty)} – replacing.");
                        ReplaceElementWithFallback(roster, i, elem, contextParty);
                        continue;
                    }

                    // Clamp weird counts to avoid TW index math surprises later.
                    int total = Math.Max(0, elem.Number);
                    int wounded = Math.Max(0, Math.Min(elem.WoundedNumber, total));
                    int xp = Math.Max(0, elem.Xp);

                    if (total != elem.Number || wounded != elem.WoundedNumber || xp != elem.Xp)
                    {
                        // Normalize counts by removing the old entry and re-adding with clamped values.
                        // Do "replace with itself" to keep the same CharacterObject.
                        SafeReplace(roster, i, elem.Character, total, wounded, xp);
                        Log.Info($"[RosterSanitizer] Normalized counts for '{elem.Character.StringId}' at index {i} " +
                                 $"(total:{elem.Number}->{total}, wounded:{elem.WoundedNumber}->{wounded}, xp:{elem.Xp}->{xp})");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[RosterSanitizer] Failed while cleaning roster");
            }
        }

        private static void ReplaceElementWithFallback(TroopRoster roster, int index, TroopRosterElement elem, MobileParty contextParty)
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
                    Log.Warn($"[RosterSanitizer] No fallback troop; removed '{elem.Character?.StringId ?? "NULL"}' x{total}.");
                    return;
                }

            // 1) Add the fallback (no wounds/xp)
            roster.AddToCounts(fallback, total, insertAtFront: false, woundedCount: 0, xpChange: 0, removeDepleted: true, index: -1);

            // 2) Remove the bad ones via the public helper (lets TW do the indexing safely)
            if (elem.Character != null)
                roster.RemoveTroop(elem.Character, total);

            Log.Info($"[RosterSanitizer] Replaced '{elem.Character?.StringId ?? "NULL"}' with '{fallback.StringId}' (count:{total}).");
        }

        private static void SafeReplace(TroopRoster roster, int index, CharacterObject newChar, int total, int wounded, int xp)
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

        private static CharacterObject GetFallbackTroop(MobileParty contextParty)
        {
            // Prefer the context party's culture basic troop if we can reach it
            CharacterObject pick = null;

            var culture = contextParty?.MapFaction?.Culture
                          ?? contextParty?.HomeSettlement?.Culture;

            try
            {
                pick = culture?.BasicTroop;
            }
            catch { }

            // Otherwise try a safe, always-present fallback
            if (pick == null)
            {
                pick = MBObjectManager.Instance?.GetObject<CharacterObject>("looter");
            }

            // Final sanity: ensure it's a valid, active CharacterObject
            return IsCharacterValid(pick) ? pick : null;
        }

        private static bool IsCharacterValid(CharacterObject c)
        {
            if (c == null) return false;

            // Wrapper knows how to detect inactive/unregistered TW objects.
            var w = new WCharacter(c);
            if (!w.IsActive) return false;

            // StringId & Name must exist
            if (string.IsNullOrWhiteSpace(c.StringId)) return false;
            if (c.Name == null) return false;

            // Tier sanity
            if (c.Tier < 0 || c.Tier > 10) return false;

            // Ensure the object manager can resolve it back
            var fromDb = MBObjectManager.Instance?.GetObject<CharacterObject>(c.StringId);
            if (!ReferenceEquals(fromDb, c) && fromDb == null) return false;

            return true;
        }

        private static string DescribeParty(MobileParty mp)
        {
            if (mp == null) return "unknown party";
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
