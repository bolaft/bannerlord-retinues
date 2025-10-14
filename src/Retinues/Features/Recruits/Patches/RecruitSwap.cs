using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Features.Recruits.Patches
{
    /// <summary>
    /// Harmony patch for OnTroopRecruited.
    /// Swaps recruited troops to best match from player faction's tree if available.
    /// </summary>
    [HarmonyPatch(
        typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior),
        "OnTroopRecruited"
    )]
    public static class RecruitSwap
    {
        [SafeMethod]
        static void Postfix(
            Hero recruiter,
            Settlement settlement,
            Hero recruitmentSource,
            CharacterObject troop,
            int count
        )
        {
            if (settlement == null)
                return; // no settlement, skip

            var faction = new WSettlement(settlement).PlayerFaction;

            if (faction == null)
                return; // not player faction settlement, skip

            if (Config.VassalLordsCanRecruitCustomTroops == false)
                return; // feature disabled

            if (recruiter?.PartyBelongedTo == null || count <= 0 || troop == null)
                return; // never touch suspicious input

            var recruiterIsPlayerFaction = new WHero(recruiter).PlayerFaction != null;
            if (Config.AllLordsCanRecruitCustomTroops == false && recruiterIsPlayerFaction == false)
                return; // not a vassal, skip

            var wt = new WCharacter(troop);
            if (!wt.IsValid)
                return; // defensive

            var root = wt.IsElite ? faction.RootElite : faction.RootBasic;
            if (root == null)
                return; // no tree, skip

            var replacement = TroopMatcher.PickBestFromTree(root, wt);
            if (replacement == null)
                return;

            // Swap in party roster
            var roster = recruiter.PartyBelongedTo.MemberRoster;
            roster.RemoveTroop(wt.Base, count);
            roster.AddToCounts(replacement.Base, count);

            Log.Debug($"RecruitSwap: {recruiter?.Name} swapped {count}x {wt} to {replacement}.");
        }
    }
}
