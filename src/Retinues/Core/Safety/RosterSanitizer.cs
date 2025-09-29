using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Party;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

static class RosterSanitizer
{
    public static void CleanParty(MobileParty mp)
    {
        try
        {
            if (mp?.Party == null) return;

            var party = mp.Party;

            var newMembers = RebuildSafe(party.MemberRoster, party);
            var newPrison  = RebuildSafe(party.PrisonRoster, party);

            TrySwapRoster(party, newMembers,  member: true);
            TrySwapRoster(party, newPrison,   member: false);
        }
        catch (Exception e)
        {
            Log.Exception(e, $"[RosterSanitizer] Failed to sanitize party {mp?.StringId }.");
        }
    }

    private static TroopRoster RebuildSafe(TroopRoster src, PartyBase owner)
    {
        try
            {
            if (src == null) return null;

            var dst = new TroopRoster(owner);

            // Only keep valid, non-stub entries
            var elems = src.GetTroopRoster();
            for (int i = 0; i < elems.Count; i++)
            {
                var e = elems[i];
                var ch = e.Character;
                if (ch == null || e.Number <= 0) continue;

                bool customInactive = false;
                try
                {
                    var w = new WCharacter(ch);
                    customInactive = w.IsCustom && !w.IsActive;
                }
                catch { /* be defensive; if wrapper explodes, keep the troop */ }

                if (customInactive)
                {
                    Log.Warn($"[RosterSanitizer] Dropping inactive custom troop {ch.Name} from {owner?.Name?.ToString() }.");
                    continue; // drop stubs
                }
                dst.AddToCounts(ch, e.Number);
            }

            return dst;
        }
        catch (Exception e)
        {
            Log.Exception(e, $"[RosterSanitizer] Failed to rebuild roster for party {owner?.Name?.ToString() }.");
            return src;
        }
    }

    private static void TrySwapRoster(PartyBase party, TroopRoster newRoster, bool member)
    {
        if (newRoster == null) return;
        var prop = member ? "MemberRoster" : "PrisonRoster";
        Reflector.SetPropertyValue(party, prop, newRoster);
    }
}
