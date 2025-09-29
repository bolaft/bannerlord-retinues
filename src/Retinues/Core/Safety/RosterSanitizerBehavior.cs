using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Safety
{
    static class RosterSanitizer
    {
        public static int CleanRoster(TroopRoster roster)
        {
            if (roster == null) return 0;

            // Take a snapshot of valid stacks
            var valid = new List<TroopRosterElement>();

            foreach (var e in roster.GetTroopRoster())
                try
                {
                    if (e.Character == null)
                        Retinues.Core.Utils.Log.Warn("[RosterSanitizer] Found null troop stack in roster; removing.");
                    else
                    {
                        valid.Add(e);
                    }
                }
                catch
                {
                    // Just in case accessing e.Character throws
                    Retinues.Core.Utils.Log.Warn("[RosterSanitizer] Found invalid troop stack in roster; removing.");
                }

            int removed = roster.TotalManCount - valid.Sum(v => v.Number);

            // Clear and re-add valid stacks
            roster.Clear(); // works on TroopRoster; clears members & wounded

            foreach (var v in valid)
            {
                // Add the healthy ones
                if (v.Number - v.WoundedNumber > 0)
                    roster.AddToCounts(v.Character, v.Number, insertAtFront: false, woundedCount: v.WoundedNumber, xpChange: v.Xp);
            }

            return removed;
        }

        public static void CleanParty(MobileParty mp)
        {
            if (mp == null) return;
            var removedMembers  = CleanRoster(mp.MemberRoster);
            var removedPrisoners = CleanRoster(mp.PrisonRoster);

            if (removedMembers + removedPrisoners > 0)
                Retinues.Core.Utils.Log.Warn($"[RosterSanitizer] {mp.Name}: removed {removedMembers} members, {removedPrisoners} prisoners with null troops.");
        }
    }
}