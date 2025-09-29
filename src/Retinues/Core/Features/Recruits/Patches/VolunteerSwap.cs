using System;
using HarmonyLib;
using Retinues.Core.Features.Recruits;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Settlements;

[HarmonyPatch(
    typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior),
    "UpdateVolunteersOfNotablesInSettlement"
)]
public static class VolunteerSwap
{
    static void Postfix(Settlement settlement)
    {
        if (settlement == null)
            return;

        if (settlement.IsHideout)
            return; // no volunteers in hideouts

        try
        {
            var clan = settlement?.OwnerClan;
            var kingdom = clan?.Kingdom;

            var playerClan = Player.Clan;
            var playerKingdom = Player.Kingdom;

            bool didSwap = false;

            if (Config.GetOption<bool>("ClanTroopsOverKingdomTroops"))
            {
                // Try clan first, then kingdom
                if (playerClan != null && clan != null && clan?.StringId == playerClan?.StringId)
                    didSwap = SwapVolunteers(settlement, playerClan);

                if (!didSwap && playerKingdom != null && kingdom != null && kingdom?.StringId == playerKingdom?.StringId)
                    didSwap = SwapVolunteers(settlement, playerKingdom);
            }
            else
            {
                // Try kingdom first, then clan
                if (playerKingdom != null && kingdom != null && kingdom?.StringId == playerKingdom?.StringId)
                    didSwap = SwapVolunteers(settlement, playerKingdom);

                if (!didSwap && playerClan != null && clan != null && clan?.StringId == playerClan?.StringId)
                    didSwap = SwapVolunteers(settlement, playerClan);
            }

            if (didSwap)
                Log.Debug($"VolunteerSwap: Swapped volunteers in {settlement?.Name}.");
        }
        catch (Exception e)
        {
            Log.Exception(e);
            return;
        }
    }

    static bool SwapVolunteers(Settlement settlement, WFaction faction)
    {
        if (settlement == null || faction == null)
            return false;

        // no custom tree, nothing to do
        if ((faction?.EliteTroops?.Count ?? 0) == 0 && (faction?.BasicTroops?.Count ?? 0) == 0)
            return false;

        var notables = settlement?.Notables; // could be null on some settlements
        if (notables == null || notables.Count == 0)
            return false;

        foreach (var notable in notables)
        {
            try
            {
                var arr = notable?.VolunteerTypes;
                if (arr == null || arr.Length == 0)
                    continue;

                for (int i = 0; i < arr.Length; i++)
                {
                    var vanilla = arr[i];
                    if (vanilla == null)
                        continue;

                    var wVanilla = new WCharacter(vanilla);

                    if (TroopSwapHelper.IsFactionTroop(faction, wVanilla))
                        continue;

                    var root = TroopSwapHelper.GetFactionRootFor(wVanilla, faction);
                    if (root == null)
                        continue;

                    var replacement = TroopSwapHelper.MatchTier(root, wVanilla.Tier);
                    if (replacement == null || replacement.StringId == wVanilla.StringId || !replacement.IsActive)
                        continue;
                    if (replacement?.Base != null)
                        arr[i] = replacement.Base;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
                // continue to next notable
            }
        }

        return true;
    }
}
