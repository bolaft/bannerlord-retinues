using System;
using HarmonyLib;
using Retinues.Compatibility;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Features.Volunteers.Patches
{
    // ATC changes the source immediately before this method. Normalize its offer
    // before Bannerlord computes the price, consumes the slot and adds the soldier.
    [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "ApplyInternal")]
    internal static class ATCVolunteerRecruitmentPatch
    {
        private static void Prefix(MobileParty side1Party, Settlement settlement,
            Hero individual, ref CharacterObject troop, int bitCode)
        {
            try
            {
                if (side1Party == null || side1Party.IsMainParty || side1Party.LeaderHero == null
                    || troop == null || troop.IsHero || settlement == null
                    || individual?.VolunteerTypes == null || bitCode < 0
                    || bitCode >= individual.VolunteerTypes.Length || individual.VolunteerTypes[bitCode] != troop)
                    return;
                var role = ATCRecruitment.GetRole(troop, settlement, individual, side1Party);
                if (role != ATCRecruitRole.Basic && role != ATCRecruitRole.Elite)
                    return;
                var source = new WCharacter(troop);
                var ws = new WSettlement(settlement);
                var sphere = ws.PlayerFaction;
                bool vassal = new WHero(side1Party.LeaderHero).PlayerFaction != null;
                WFaction faction = null;
                if (sphere != null && (Config.AllLordsCanRecruitCustomTroops
                    || Config.VassalLordsCanRecruitCustomTroops && vassal))
                    faction = sphere;
                else if (vassal && Config.VassalLordsRecruitCustomTroopsAnywhere)
                    faction = Player.Clan ?? sphere;
                if (faction == null || Config.RestrictToSameCultureSettlements && faction.Culture != ws.Culture)
                    return;
                troop = ATCRecruitment.MatchVolunteer(faction, source, role)?.Base ?? troop;
            }
            catch (Exception e) { Log.Exception(e, "[ATC] AI volunteer replacement failed; retaining original recruit."); }
        }
    }
}