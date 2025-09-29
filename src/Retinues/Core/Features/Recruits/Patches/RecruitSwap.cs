using System;
using HarmonyLib;
using Retinues.Core.Features.Recruits;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

[HarmonyPatch(
    typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior),
    "OnTroopRecruited"
)]
public static class RecruitSwap
{
    static void Postfix(
        Hero recruiter,
        TaleWorlds.CampaignSystem.Settlements.Settlement settlement,
        Hero recruitmentSource,
        CharacterObject troop,
        int count
    )
    {
        try
        {
            if (troop == null || count <= 0)
                return;

            if (recruiter?.PartyBelongedTo == null || troop == null || count <= 0)
                return;

            var targetFaction = TroopSwapHelper.ResolveTargetFaction(recruiter);
            if (targetFaction == null)
                return;

            // Wrap the vanilla recruit
            var vanilla = new WCharacter(troop);

            // Find the best custom equivalent for this faction
            var replacement = TroopSwapHelper.FindReplacement(vanilla, targetFaction);
            if (replacement == null || replacement.StringId == vanilla.StringId || !replacement.IsActive)
                return;

            var roster = recruiter.PartyBelongedTo.MemberRoster;

            Log.Info(
                $"RecruitSwap: Swapping {count}x {troop.Name} for {replacement.Name} in {recruiter.Name}'s party."
            );

            // Swap stacks
            roster.AddToCounts(troop, -count);
            roster.AddToCounts(replacement.Base, count);
        }
        catch (Exception e)
        {
            Log.Exception(e);
            return;
        }
    }
}
