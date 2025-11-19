using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Configuration;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Features.Scenes.Patches
{
    /// <summary>
    /// Patch entry for dynamic swapping of ambiant NPCs in settlement missions.
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

                // Get player faction
                var faction = WSettlement.Current?.PlayerFaction;
                if (faction == null)
                    return; // No faction, nothing to do

                // Wrap source character
                var troop = AgentHelper.TroopFromAgentBuildData(agentBuildData, origin: true);
                if (troop.IsHero)
                    return; // Don't affect heroes
                if (troop.IsCustom)
                    return; // Don't affect custom troops

                // Try to pick best replacement from faction
                WCharacter replacement = TroopMatcher.PickBestFromFaction(
                    faction,
                    troop,
                    sameTierOnly: false
                );

                // If we found a different replacement, clone AgentBuildData with it
                if (replacement != null)
                {
                    agentBuildData = ReplaceCharacterInBuildData(agentBuildData, replacement);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// Replace the character in the given AgentBuildData with the given WCharacter.
        /// Keeps the original equipment for non-armor pieces.
        /// </summary>
        private static AgentBuildData ReplaceCharacterInBuildData(
            AgentBuildData src,
            WCharacter replacement
        )
        {
            // Swap character
            src.Character(replacement.Base);

            // Use replacement battle armor set
            var eq = Equipment.CreateFromEquipmentCode(replacement.Loadout.Battle.Code);

            foreach (var slot in WEquipment.Slots)
            {
                if (!WEquipment.IsArmorSlot(slot))
                    continue;

                var piece = eq[slot];

                if (piece.Item != null)
                {
                    // Replacement HAS an armor piece: override it
                    src.AgentOverridenSpawnEquipment[slot] = piece;
                }
                else
                {
                    // Replacement does NOT have an armor piece: clear original one
                    src.AgentOverridenSpawnEquipment[slot] = EquipmentElement.Invalid;
                }
            }

            return src;
        }
    }
}
