using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Behaviors.Experience.Patches
{
    /// <summary>
    /// Marks party-wide XP awards so hideout reserves cannot generate skill points.
    /// The original awards still run unchanged: this only adjusts Retinues' XP capture.
    /// </summary>
    [HarmonyPatch]
    internal static class HideoutSharedXpPatch
    {
        [ThreadStatic]
        private static PartyBase _sharedXpParty;

        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            // Baptised in Blood grants XP per infantry soldier in the ENTIRE party
            // on each hero kill, even when those soldiers are outside the hideout.
            yield return AccessTools.Method(typeof(BattleCampaignBehavior), "OnHeroCombatHit");
            yield return AccessTools.Method(typeof(MobilePartyHelper), nameof(MobilePartyHelper.PartyAddSharedXp));
        }

        [HarmonyPrefix]
        private static void Prefix(object party, out PartyBase __state)
        {
            __state = _sharedXpParty;
            _sharedXpParty = party is MobileParty mobile ? mobile.Party : party as PartyBase;
        }

        [HarmonyFinalizer]
        private static void Finalizer(PartyBase __state)
        {
            // Restore nested awards too, including when another mod or the game throws.
            _sharedXpParty = __state;
        }

        internal static int GetSkillPointXp(PartyBase party, CharacterObject troop, int xp, int stackSize)
        {
            if (party == null || party != _sharedXpParty
                || party.MapEvent?.IsHideoutBattle != true || party.MapEvent.IsPlayerSimulation)
                return xp;

            if (xp <= 0 || stackSize <= 0)
                return 0;

            int participating = 0;
            var side = party.MapEventSide;
            foreach (var battleParty in side.Parties)
            {
                if (battleParty.Party != party || battleParty.Troops == null)
                    continue;

                foreach (var soldier in battleParty.Troops)
                {
                    // Allocation identifies the actual hideout party on BL 1.2 as well,
                    // where the battle roster still includes unselected reserves.
                    if (soldier.Troop == troop && !soldier.IsKilled
                        && side.GetAllocatedTroopParty(soldier.Descriptor) == party)
                        participating++;
                }
            }

            // Do not scale direct combat XP: only these party-wide grants include reserves.
            return (int)((long)xp * Math.Min(participating, stackSize) / stackSize);
        }
    }
}
