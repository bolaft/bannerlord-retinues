using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Safety
{
    public static class RosterSanitizer
    {
        public static void CleanRoster(TroopRoster roster)
        {
            if (roster == null) return;

            try
            {
                for (int i = roster.Count - 1; i >= 0; i--)
                {
                    var element = roster.GetElementCopyAtIndex(i);
                    if (element.Character == null)
                    {
                        Log.Warn($"[RosterSanitizer] Null troop at index {i}, removing.");
                        PruneElement(roster, element, i);
                        continue;
                    }

                    var wChar = new WCharacter(element.Character);

                    // Any custom troop stub (inactive) must not exist in rosters
                    if (wChar.IsCustom && !wChar.IsActive)
                    {
                        Log.Warn($"[RosterSanitizer] Removing inactive custom troop {wChar?.StringId} at index {i}.");
                        PruneElement(roster, element, i);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[RosterSanitizer] Failed while cleaning roster");
            }
        }

        private static void PruneElement(TroopRoster roster, TroopRosterElement element, int index)
        {
            try
            {
                var total = element.Number;
                var wounded = element.WoundedNumber;
                var xp = element.Xp;

                // Fallback replacement troop (always valid in vanilla)
                var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");

                if (element.Character == null || (new WCharacter(element.Character)).IsCustom == false)
                {
                    // If null, just remove safely
                    roster.AddToCounts(
                        element.Character,
                        -total,
                        insertAtFront: false,
                        woundedCount: -wounded,
                        xpChange: -xp,
                        removeDepleted: true,
                        index: index
                    );
                    Log.Info($"[RosterSanitizer] Removed NULL element at {index}");
                }
                else
                {
                    // Replace invalid custom troop with looter
                    roster.AddToCounts(
                        element.Character,
                        -total,
                        insertAtFront: false,
                        woundedCount: -wounded,
                        xpChange: -xp,
                        removeDepleted: true,
                        index: index
                    );

                    roster.AddToCounts(
                        looter,
                        total,
                        insertAtFront: false,
                        woundedCount: wounded,
                        xpChange: xp,
                        removeDepleted: true,
                        index: index
                    );

                    Log.Info($"[RosterSanitizer] Replaced {element.Character?.StringId ?? "NULL"} with {looter.StringId} " +
                            $"(total:{total}, wounded:{wounded}, xp:{xp})");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[RosterSanitizer] Failed pruning/replacing element");
            }
        }

        public static void CleanParty(MobileParty mp)
        {
            if (mp == null) return;

            CleanRoster(mp?.MemberRoster);
            CleanRoster(mp?.PrisonRoster);
        }
    }
}
