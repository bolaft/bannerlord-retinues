using HarmonyLib;
using Retinues.Core.Game.Helpers;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Features.Recruits.Patches
{
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
            if (recruiter?.PartyBelongedTo == null || count <= 0 || troop == null)
                return; // never touch suspicious input

            var wt = new WCharacter(troop);
            if (!wt.IsValid)
                return; // defensive

            var faction = new WHero(recruiter).PlayerFaction;
            if (faction == null)
                return; // non-player faction, skip

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

            Log.Info(
                $"RecruitSwap: {recruiter?.Name} swapped {count}x {wt?.StringId} to {replacement?.StringId}."
            );
        }
    }
}
