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
        // Cache reflection to the private helper once
        private static readonly MethodInfo _addTroopMI =
            typeof(Settlement).GetMethod("AddTroopToMilitiaParty",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static void AddLane(Settlement s, MobileParty party,
                                    CharacterObject basic, CharacterObject elite,
                                    float ratio, int total)
        {
            if ((basic == null && elite == null) || _addTroopMI == null) return;
            _addTroopMI.Invoke(s, [party, basic, elite, ratio, total]);
        }

        // Prefix: if settlement belongs to player side, use WSettlement militia, then skip original
        static bool Prefix(Settlement __instance, MobileParty militiaParty, int militiaNumberToAdd)
        {
            try
            {
                if (__instance == null || militiaParty == null || militiaNumberToAdd <= 0)
                    return true; // let vanilla handle bad inputs

                var ws = new WSettlement(__instance);

                // If your wrapper resolved no player-side faction (not player clan/kingdom), let vanilla run
                if (ws.Faction == null)
                    return true;

                // Compute vanilla split ratios (melee/ranged)
                Campaign.Current.Models.SettlementMilitiaModel
                    .CalculateMilitiaSpawnRate(__instance, out float meleeRatio, out float rangedRatio);

                // Pull your choices via wrapper (these already fall back to culture if custom is inactive)
                var melee       = ws.MilitiaMelee;
                var meleeElite  = ws.MilitiaMeleeElite;
                var ranged      = ws.MilitiaRanged;
                var rangedElite = ws.MilitiaRangedElite;

                if (melee == null || meleeElite == null || ranged == null || rangedElite == null)
                    return true; // no custom militia set, let vanilla run
                
                if (!melee.IsActive || !meleeElite.IsActive || !ranged.IsActive || !rangedElite.IsActive)
                    return true; // inactive custom troops, let vanilla run

                // Spawn using the same internal helper the game uses
                AddLane(__instance, militiaParty, melee.Base,  meleeElite.Base,  meleeRatio,  militiaNumberToAdd);
                AddLane(__instance, militiaParty, ranged.Base, rangedElite.Base, rangedRatio, militiaNumberToAdd);

                Log.Debug($"[MilitiaPatch] Custom militia for {ws.Name}: " +
                         $"melee={melee?.StringId}/{meleeElite?.StringId}, " +
                         $"ranged={ranged?.StringId}/{rangedElite?.StringId}, add={militiaNumberToAdd}");

                // We handled it—skip original
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MilitiaPatch] Failed; falling back to vanilla");
                return true;
            }
        }
    }
}
