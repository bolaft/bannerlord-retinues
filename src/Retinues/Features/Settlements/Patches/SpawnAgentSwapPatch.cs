using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Settlements.Patches
{
    /// <summary>
    /// Patch entry for dynamic civilian/guard swapping in settlement missions.
    /// </summary>
    [HarmonyPatch(typeof(Mission))]
    internal static class SpawnAgentSwapPatch
    {
        // Prefix so we can swap the origin before AgentBuildData is created.
        [HarmonyPatch(nameof(Mission.SpawnAgent), [typeof(AgentBuildData), typeof(bool)])]
        [HarmonyPrefix]
        private static void Prefix(ref AgentBuildData agentBuildData, bool spawnFromAgentVisuals)
        {
            try
            {
                if (Config.ReplaceAmbientNPCs == false)
                    return; // Feature disabled

                // Get Mission
                Mission m = Mission.Current;

                if (m == null)
                    return; // No mission, nothing to do

                List<MissionMode> blockedModes =
                [
                    MissionMode.Tournament,
                    MissionMode.Duel,
                    MissionMode.Stealth,
                    MissionMode.CutScene,
                    MissionMode.Deployment,
                    MissionMode.Battle,
                ];

                if (blockedModes.Contains(m.Mode))
                    return; // Don't affect these mission modes

                // Get current settlement
                var s = Settlement.CurrentSettlement;
                if (s == null)
                    return; // No settlement, nothing to do

                // Get player faction
                var faction = new WSettlement(s).PlayerFaction;
                if (faction == null)
                    return; // No faction, nothing to do

                // Get basic character from AgentBuildData
                var basic = agentBuildData.AgentOrigin?.Troop;
                if (basic == null)
                    return; // No basic character, nothing to do

                // Direct cast
                if (basic is not CharacterObject co)
                    return; // No character object, nothing to do

                // Wrap source character
                var sourceChar = new WCharacter(co);
                if (sourceChar.IsHero)
                    return; // Don't affect heroes
                if (sourceChar.IsCustom)
                    return; // Don't affect custom troops

                // Try to pick best replacement from faction
                WCharacter replacement =
                    TroopMatcher.PickSpecialFromFaction(faction, sourceChar)
                    ?? TroopMatcher.PickBestFromFaction(faction, sourceChar)
                    ?? sourceChar;

                // If we found a different replacement, clone AgentBuildData with it
                if (replacement != null)
                    agentBuildData = AgentHelper.ReplaceCharacterInBuildData(
                        agentBuildData,
                        replacement.Base
                    );
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }
}
