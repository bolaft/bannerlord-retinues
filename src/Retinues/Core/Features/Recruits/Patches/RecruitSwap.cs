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
            Hero hero,
            Settlement settlement,
            Hero recruitmentSource,
            CharacterObject co,
            int count
        )
        {
            if (hero?.PartyBelongedTo == null || count <= 0 || co == null)
                return; // never touch suspicious input

            var troop = new WCharacter(co);
            if (!troop.IsValid)
                return; // defensive

            var faction = new WHero(hero).PlayerFaction;
            if (faction == null)
                return; // non-player faction, skip

            var root = troop.IsElite ? faction.RootElite : faction.RootBasic;
            if (root == null)
                return; // no tree, skip

            var replacement = TroopMatcher.PickBestFromTree(root, troop);
            if (replacement == null)
                return;

            // Swap in party roster
            var roster = hero.PartyBelongedTo.MemberRoster;
            roster.RemoveTroop(troop.Base, count);
            roster.AddToCounts(replacement.Base, count);

            Log.Info(
                $"RecruitSwap: {hero?.Name} swapped {count}x {troop?.StringId} → {replacement?.StringId}."
            );
        }
    }
}
