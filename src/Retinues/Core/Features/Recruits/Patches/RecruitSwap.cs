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
    [SafeMethod]
    static void Postfix(
        Hero recruiter,
        TaleWorlds.CampaignSystem.Settlements.Settlement settlement,
        Hero recruitmentSource,
        CharacterObject troop,
        int count
    )
    {
        if (recruiter?.PartyBelongedTo == null || count <= 0)
            return;
        if (TroopSwapHelper.LooksCorrupt(troop))
            return; // never touch suspicious input

        var faction = TroopSwapHelper.ResolveTargetFaction(recruiter);
        if (faction == null)
            return;

        var vanilla = new WCharacter(troop);
        if (!TroopSwapHelper.IsValid(vanilla))
            return; // defensive

        var replacement = TroopSwapHelper.FindReplacement(vanilla, faction);
        if (!TroopSwapHelper.IsValid(replacement))
            return;

        // Perform atomic stack swap
        var roster = recruiter.PartyBelongedTo.MemberRoster;
        roster.RemoveTroop(troop, count);
        roster.AddToCounts(replacement.Base, count);

        Log.Info(
            $"RecruitSwap: {recruiter?.Name} swapped {count}x {troop?.StringId} → {replacement?.StringId}."
        );
    }
}
