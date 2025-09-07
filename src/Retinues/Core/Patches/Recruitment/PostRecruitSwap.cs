using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using Retinues.Core.Logic;
using Retinues.Core.Wrappers.Campaign;
using Retinues.Core.Patches.Recruitment.Helpers;

[HarmonyPatch(typeof(RecruitmentCampaignBehavior), "OnTroopRecruited")]
public static class PostRecruitSwapPatch
{
    static void Postfix(Hero recruiter, Settlement settlement, Hero recruitmentSource, CharacterObject troop, int count)
    {
        if (recruiter?.PartyBelongedTo == null || troop == null || count <= 0)
            return;

        WFaction playerClan = Player.Clan;
        WFaction playerKingdom = Player.Kingdom;

        // Decide which custom tree to use:
        // 1) If recruiter is in player clan → clan tree
        // 2) else if recruiter is in player kingdom → kingdom tree
        WFaction target = null;
        if (recruiter.Clan != null && recruiter.Clan.StringId == playerClan?.StringId)
            target = playerClan;
        else if (recruiter.Clan?.Kingdom != null && recruiter.Clan.Kingdom.StringId == playerKingdom?.StringId)
            target = playerKingdom;

        if (target == null) return;

        // Skip if it’s already one of our custom troops
        if (RecruitmentHelpers.IsFactionTroop(target, troop))
            return;

        var root = RecruitmentHelpers.GetFactionRootFor(troop, target);
        if (root == null) return;

        var replacement = RecruitmentHelpers.TryToLevel(root, troop.Tier);
        if (replacement == null || replacement == troop) return;

        var roster = recruiter.PartyBelongedTo.MemberRoster;

        // Remove the freshly recruited vanilla stack and add the matching custom stack.
        roster.AddToCounts(troop, -count);
        roster.AddToCounts(replacement, count);
    }
}
