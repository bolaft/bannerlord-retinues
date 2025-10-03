using System;
using HarmonyLib;
using Retinues.Core.Game;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Features.Recruits.Patches
{
    [HarmonyPatch(
        typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior),
        "UpdateVolunteersOfNotablesInSettlement"
    )]
    public static class VolunteerSwap
    {
        [SafeMethod]
        static void Postfix(Settlement settlement)
        {
            SwapVolunteersInSettlement(settlement);
        }

        public static bool SwapVolunteersInSettlement(Settlement settlement)
        {
            if (settlement == null || settlement.IsHideout)
                return false;

            var clan = settlement.OwnerClan;
            var kingdom = clan?.Kingdom;
            var playerClan = Player.Clan;
            var playerKingdom = Player.Kingdom;

            bool preferClan = Config.GetOption<bool>("ClanTroopsOverKingdomTroops");
            bool did = false;

            if (preferClan)
            {
                if (
                    !did
                    && playerClan != null
                    && clan != null
                    && playerClan.StringId == clan.StringId
                )
                    did = SwapVolunteers(settlement, playerClan);
                if (
                    !did
                    && playerKingdom != null
                    && kingdom != null
                    && playerKingdom.StringId == kingdom.StringId
                )
                    did = SwapVolunteers(settlement, playerKingdom);
            }
            else
            {
                if (
                    !did
                    && playerKingdom != null
                    && kingdom != null
                    && playerKingdom.StringId == kingdom.StringId
                )
                    did = SwapVolunteers(settlement, playerKingdom);
                if (
                    !did
                    && playerClan != null
                    && clan != null
                    && playerClan.StringId == clan.StringId
                )
                    did = SwapVolunteers(settlement, playerClan);
            }

            if (did)
                Log.Debug($"VolunteerSwap: swapped volunteer lists in {settlement.Name}.");

            return did;
        }

        private static bool SwapVolunteers(Settlement settlement, WFaction faction)
        {
            if (Config.GetOption<bool>("SwapOnlyForCorrectCulture"))
                if (settlement.Culture?.StringId != faction.Culture?.StringId)
                    return false;

            if (settlement?.Notables == null || settlement.Notables.Count == 0)
                return false;
            if ((faction?.EliteTroops?.Count ?? 0) == 0 && (faction?.BasicTroops?.Count ?? 0) == 0)
                return false;

            foreach (var notable in settlement.Notables)
            {
                try
                {
                    var arr = notable?.VolunteerTypes;
                    if (arr == null || arr.Length == 0)
                        continue;

                    for (int i = 0; i < arr.Length; i++)
                    {
                        var vanilla = arr[i];

                        // If TW fed us partially initialized entries, hard-fix to safe vanilla baseline.
                        if (TroopSwapHelper.LooksCorrupt(vanilla))
                        {
                            var safe = TroopSwapHelper.SafeVanillaFallback(settlement);
                            if (TroopSwapHelper.IsValidChar(safe))
                                arr[i] = safe;
                            vanilla = safe;
                        }

                        var wVanilla = new WCharacter(vanilla);
                        if (!TroopSwapHelper.IsValid(wVanilla))
                            continue;
                        if (TroopSwapHelper.IsFactionTroop(faction, wVanilla))
                            continue;

                        var root = TroopSwapHelper.GetFactionRootFor(wVanilla, faction);
                        if (!TroopSwapHelper.IsValid(root))
                            continue;

                        var repl = TroopSwapHelper.MatchTier(root, wVanilla.Tier);
                        if (TroopSwapHelper.IsValid(repl))
                            arr[i] = repl.Base;
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(
                        e,
                        $"VolunteerSwap: notable {notable?.Name} in {settlement?.Name}"
                    );
                }
            }
            return true;
        }
    }
}
