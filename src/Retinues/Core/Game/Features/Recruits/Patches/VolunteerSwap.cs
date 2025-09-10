using System.Linq;
using HarmonyLib;
using Retinues.Core.Game;
using Retinues.Core.Game.Helpers;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

[HarmonyPatch(typeof(RecruitmentCampaignBehavior), "UpdateVolunteersOfNotablesInSettlement")]
public static class VolunteerSwap
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
            Log.Debug(
                $"Swapped volunteers in {settlement.Name} for clan {playerClan.Name} troops."
            );

        if (swapped)
            return; // Already swapped for clan, no need to check kingdom

        // Next, check if the settlement is in the player kingdom (if any)
        if (kingdom is not null && kingdom.StringId == playerKingdom?.StringId)
            swapped = SwapTroopsInSettlement(playerKingdom, settlement);

        if (swapped)
            Log.Debug(
                $"Swapped volunteers in {settlement.Name} for kingdom {playerKingdom.Name} troops."
            );
    }

    static bool SwapTroopsInSettlement(WFaction faction, Settlement settlement)
    {
        if (faction.EliteTroops.Count == 0 && faction.BasicTroops.Count == 0)
            return false; // Faction has no custom troops, nothing to do

        foreach (var notable in settlement.Notables.ToList())
        {
            if (!notable.CanHaveRecruits)
                continue;

            for (int i = 0; i < notable.VolunteerTypes.Length; i++)
            {
                var vanilla = notable.VolunteerTypes[i];
                if (vanilla == null)
                    continue;

                if (CharacterObjectHelper.IsFactionTroop(faction, vanilla))
                    continue;

                var rootId = CharacterObjectHelper.IsEliteLine(vanilla)
                    ? faction.RootElite?.StringId
                    : faction.RootBasic?.StringId;
                if (rootId == null)
                    continue;
                var root = MBObjectManager.Instance.GetObject<CharacterObject>(rootId);
                if (root == null)
                    continue;

                notable.VolunteerTypes[i] = CharacterObjectHelper.TryToLevel(root, vanilla.Tier);
            }
        }

        return true;
    }
}
