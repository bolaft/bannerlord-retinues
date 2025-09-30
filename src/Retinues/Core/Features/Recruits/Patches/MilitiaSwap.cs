using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Features.Recruits.Patches
{
    [HarmonyPatch(typeof(Settlement), "AddMilitiasToParty")]
    internal static class PlayerMilitiaSpawnPatch
    {
        private static readonly MethodInfo _addTroopMI =
            typeof(Settlement).GetMethod("AddTroopToMilitiaParty", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void AddLane(Settlement s, MobileParty party, CharacterObject basic, CharacterObject elite, float ratio, int total)
        {
            if (_addTroopMI == null) return;
            if (!TroopSwapHelper.IsValidChar(basic) && !TroopSwapHelper.IsValidChar(elite)) return;
            _addTroopMI.Invoke(s, [party, basic, elite, ratio, total]);
        }

        static bool Prefix(Settlement __instance, MobileParty militiaParty, int militiaNumberToAdd)
        {
            try
            {
                if (__instance == null || militiaParty == null || militiaNumberToAdd <= 0) return true;

                var ws = new WSettlement(__instance);
                if (ws.Faction == null) return true; // not player side → vanilla

                // vanilla ratios
                Campaign.Current.Models.SettlementMilitiaModel
                    .CalculateMilitiaSpawnRate(__instance, out float meleeRatio, out float rangedRatio);

                var melee       = ws.MilitiaMelee;
                var meleeElite  = ws.MilitiaMeleeElite;
                var ranged      = ws.MilitiaRanged;
                var rangedElite = ws.MilitiaRangedElite;

                // if any lane completely missing/invalid → fallback to vanilla
                if (!(TroopSwapHelper.IsValid(melee) && TroopSwapHelper.IsValid(meleeElite)
                      && TroopSwapHelper.IsValid(ranged) && TroopSwapHelper.IsValid(rangedElite)))
                    return true;

                AddLane(__instance, militiaParty, melee.Base,  meleeElite.Base,  meleeRatio,  militiaNumberToAdd);
                AddLane(__instance, militiaParty, ranged.Base, rangedElite.Base, rangedRatio, militiaNumberToAdd);

                Log.Debug($"[MilitiaPatch] {ws.Name}: custom militia used (add={militiaNumberToAdd}).");
                return false; // handled
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MilitiaPatch] failure → fall back to vanilla");
                return true;
            }
        }
    }
}
