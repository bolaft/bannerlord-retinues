using System;
using TaleWorlds.CampaignSystem.Party;
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
                        Log.Warn($"[RosterSanitizer] Removing inactive custom troop {wChar.StringId} at index {i}.");
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

                if (total > 0)
                {
                    roster.AddToCounts(
                        element.Character,
                        -total,
                        insertAtFront: false,
                        woundedCount: -wounded,
                        xpChange: -xp,
                        removeDepleted: true,
                        index: index
                    );
                }

                Log.Info($"[RosterSanitizer] Pruned {element.Character?.StringId ?? "NULL"} (total:{total}, wounded:{wounded}, xp:{xp})");
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[RosterSanitizer] Failed pruning element");
            }
        }

        public static void CleanParty(MobileParty mp)
        {
            if (mp == null) return;

            CleanRoster(mp.MemberRoster);
            CleanRoster(mp.PrisonRoster);
        }
    }
}
