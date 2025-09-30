using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Features.Recruits.Patches
{
    [HarmonyPatch(typeof(Settlement), "AddMilitiasToParty")]
    internal static class PlayerMilitiaSpawnPatch
    {
        private static readonly MethodInfo Helper_RefInt = typeof(Settlement)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(m =>
            {
                if (m.Name != "AddTroopToMilitiaParty") return false;
                var p = m.GetParameters();
                return p.Length == 5
                       && p[0].ParameterType == typeof(MobileParty)
                       && p[1].ParameterType == typeof(CharacterObject)
                       && p[2].ParameterType == typeof(CharacterObject)
                       && p[3].ParameterType == typeof(float)
                       && p[4].ParameterType.IsByRef
                       && p[4].ParameterType.GetElementType() == typeof(int);
            });

        static bool Prefix(
            Settlement __instance,
            [HarmonyArgument(0)] MobileParty militaParty,
            [HarmonyArgument(1)] int militiaToAdd)
        {
            if (Helper_RefInt == null || __instance == null || militaParty == null || militiaToAdd <= 0)
                return true;

            try
            {
                var ws = new WSettlement(__instance);

                if (ws.Faction == null)
                    return true; // no faction -> vanilla
                
                Log.Info($"[MilitiaPatch] Settlement {ws.Name} faction={ws.Faction.Name} add={militiaToAdd}");

                // pull custom militia; if any is missing/inactive, leave vanilla behavior
                var melee       = ws.MilitiaMelee;
                var meleeElite  = ws.MilitiaMeleeElite;
                var ranged      = ws.MilitiaRanged;
                var rangedElite = ws.MilitiaRangedElite;

                bool allActive = melee?.IsActive == true
                              && meleeElite?.IsActive == true
                              && ranged?.IsActive == true
                              && rangedElite?.IsActive == true;

                if (!allActive)
                    return true; // vanilla

                // vanilla 1.2 uses calculated melee ratio and hard-coded 1f for ranged
                Campaign.Current.Models.SettlementMilitiaModel
                    .CalculateMilitiaSpawnRate(__instance, out float meleeRatio, out _);

                int remaining = militiaToAdd;

                // invoke private helper with ref int
                object[] args;

                args = [militaParty, melee.Base, meleeElite.Base, meleeRatio, remaining];
                Helper_RefInt.Invoke(__instance, args);
                remaining = (int)args[4];

                args = [militaParty, ranged.Base, rangedElite.Base, 1f, remaining];
                Helper_RefInt.Invoke(__instance, args);
                remaining = (int)args[4];

                Log.Debug($"[MilitiaPatch] Custom militia for {ws.Name} add={militiaToAdd} rem={remaining}");

                // handled -> skip original
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MilitiaPatch] Fallback to vanilla");
                return true; // let vanilla proceed on failure
            }
        }
    }
}
