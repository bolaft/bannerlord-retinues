using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace OldRetinues.Features.Volunteers.Patches
{
    /// <summary>
    /// Harmony patch for OnTroopRecruited (AI only).
    /// </summary>
    [HarmonyPatch(
        typeof(TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior),
        "OnTroopRecruited"
    )]
    public static class VolunteerSwapForAI
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
            // Basic sanity
            if (settlement == null || recruiter == null || troop == null || count <= 0)
                return;

            // Player path is handled by VolunteerSwapForPlayer
            if (recruiter.IsHumanPlayerCharacter)
                return;

            var party = recruiter.PartyBelongedTo;
            if (party == null)
                return;

            var wSettlement = new WSettlement(settlement);
            var wTroop = new WCharacter(troop);
            if (!wTroop.IsValid)
                return;

            var wHero = new WHero(recruiter);
            bool isPlayerVassal = wHero.PlayerFaction != null;

            var playerSphereFaction = wSettlement.PlayerFaction; // clan or kingdom
            bool isPlayerSphereSettlement = playerSphereFaction != null;

            bool isRetinue = wTroop.IsRetinue;

            // Decide if this recruiter can get CUSTOM troops here and which faction's custom tree to use.

            WFaction customFaction = null;
            bool canRecruitCustom = false;

            // 1) All lords can recruit custom troops in PLAYER-SPHERE settlements.
            if (isPlayerSphereSettlement && Config.AllLordsCanRecruitCustomTroops)
            {
                canRecruitCustom = true;
                customFaction = playerSphereFaction ?? Player.Clan;
            }

            // 2) Vassals can recruit custom troops in their fiefs.
            if (
                !canRecruitCustom
                && isPlayerSphereSettlement
                && Config.VassalLordsCanRecruitCustomTroops
                && isPlayerVassal
            )
            {
                canRecruitCustom = true;
                customFaction = playerSphereFaction ?? Player.Clan;
            }

            // 3) Vassals recruit custom troops anywhere.
            if (
                !canRecruitCustom
                && Config.VassalLordsRecruitCustomTroopsAnywhere
                && isPlayerVassal
            )
            {
                canRecruitCustom = true;
                // For "anywhere", use the player's custom faction as the tree owner.
                customFaction ??= Player.Clan ?? playerSphereFaction;
            }

            // If we still have no custom faction to map to, this hero does not get customs.
            if (!canRecruitCustom || customFaction == null)
            {
                // We only need "swap back" behaviour when the settlement is in the player sphere;
                // outside of it, volunteers are native and there's nothing to revert.
                if (!isPlayerSphereSettlement || !isRetinue)
                    return;

                // Unauthorized lord in PLAYER fief: swap custom back to native via settlement culture.
                var culture = wSettlement.Culture;
                if (culture == null)
                    return;

                var nativeRoot = wTroop.IsElite ? culture.RootElite : culture.RootBasic;
                if (nativeRoot == null)
                    return;

                var nativeReplacement = TroopMatcher.PickBestFromTree(
                    nativeRoot,
                    wTroop,
                    sameTierOnly: false
                );
                if (nativeReplacement == null || nativeReplacement == wTroop)
                    return;

                var unauthorizedRoster = party.MemberRoster;
                unauthorizedRoster.RemoveTroop(wTroop.Base, count);
                unauthorizedRoster.AddToCounts(nativeReplacement.Base, count);

                Log.Debug(
                    $"VolunteerSwapForAI: unauthorized {recruiter?.Name} recruited {count}x {wTroop} "
                        + $"-> reverted to native {nativeReplacement}."
                );
                return;
            }

            // Authorized path: normalize to the custom tree
            // (works both in player fiefs AND "anywhere" for vassals).

            var root = wTroop.IsElite ? customFaction.RootElite : customFaction.RootBasic;
            if (root == null)
                return;

            var replacement = TroopMatcher.PickBestFromTree(root, wTroop, sameTierOnly: false);
            if (replacement == null || replacement == wTroop)
                return;

            var roster = party.MemberRoster;
            roster.RemoveTroop(wTroop.Base, count);
            roster.AddToCounts(replacement.Base, count);
        }
    }
}
