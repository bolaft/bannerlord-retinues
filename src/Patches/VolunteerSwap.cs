using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Utils;

[HarmonyPatch(typeof(RecruitmentCampaignBehavior), "UpdateVolunteersOfNotablesInSettlement")]
public static class VolunteerSwapPatch
{
    static void Postfix(Settlement settlement)
    {
        var clan = settlement?.OwnerClan;
        var kingdom = settlement?.OwnerClan?.Kingdom;

        WFaction playerClan = Player.Clan;
        WFaction playerKingdom = Player.Kingdom;

        bool swapped = false;

        // First, check if the settlement is in the player's clan
        if (clan is not null && clan.StringId == playerClan?.StringId)
            swapped = SwapTroopsInSettlement(playerClan, settlement);

        if (swapped)
            Log.Debug($"Swapped volunteers in {settlement.Name} for clan {playerClan.Name} troops.");

        if (swapped) return;  // Already swapped for clan, no need to check kingdom

        // Next, check if the settlement is in the player kingdom (if any)
        if (kingdom is not null && kingdom.StringId == playerKingdom?.StringId)
            swapped = SwapTroopsInSettlement(playerKingdom, settlement);

        if (swapped)
            Log.Debug($"Swapped volunteers in {settlement.Name} for kingdom {playerKingdom.Name} troops.");
    }

    static bool SwapTroopsInSettlement(WFaction faction, Settlement settlement)
    {
        if (faction.EliteTroops.Count == 0 && faction.BasicTroops.Count == 0)
            return false; // Faction has no custom troops, nothing to do

        foreach (var notable in settlement.Notables.ToList())
        {
            if (!notable.CanHaveRecruits) continue;

            for (int i = 0; i < notable.VolunteerTypes.Length; i++)
            {
                var vanilla = notable.VolunteerTypes[i];
                if (vanilla == null) continue;

                if (IsFactionTroop(vanilla)) continue;

                var rootId = IsEliteLine(vanilla) ? faction.RootElite.StringId : faction.RootBasic.StringId;
                var root = MBObjectManager.Instance.GetObject<CharacterObject>(rootId);
                if (root == null) continue;

                notable.VolunteerTypes[i] = TryToLevel(root, vanilla.Tier);
            }
        }

        return true;
    }

    static CharacterObject TryToLevel(CharacterObject root, int tier)
    {
        var cur = root;
        while (cur.Tier < tier && cur.UpgradeTargets != null && cur.UpgradeTargets.Length > 0)
            cur = cur.UpgradeTargets[MBRandom.RandomInt(cur.UpgradeTargets.Length)];
        return cur;
    }

    static bool IsEliteLine(CharacterObject unit)
    {
        // Walk from Culture.EliteBasicTroop across upgrades and checks membership.
        var seen = new System.Collections.Generic.HashSet<CharacterObject>();
        var stack = new System.Collections.Generic.Stack<CharacterObject>();
        var eliteRoot = unit.Culture?.EliteBasicTroop;
        if (eliteRoot == null) return false;

        stack.Push(eliteRoot);
        seen.Add(eliteRoot);
        while (stack.Count > 0)
        {
            var n = stack.Pop();
            if (n == unit) return true;
            var ups = n.UpgradeTargets;
            if (ups == null) continue;
            foreach (var v in ups) if (seen.Add(v)) stack.Push(v);
        }
        return false;
    }

    static bool IsFactionTroop(CharacterObject c)
    {
        foreach (var troop in Enumerable.Concat(Player.Clan.BasicTroops, Player.Clan.EliteTroops))
            if (troop.StringId == c.StringId)
                return true;

        return false;
    }
}
