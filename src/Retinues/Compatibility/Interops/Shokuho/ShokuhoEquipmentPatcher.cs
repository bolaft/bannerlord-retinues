using System;
using System.Reflection;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Compatibility.Interops.Shokuho
{
    /// <summary>
    /// Compatibility patch to skip Shokuho's agent-equipment logic for edited/custom troops.
    /// </summary>
    internal static class ShokuhoEquipmentPatcher
    {
        /// <summary>
        /// Fully-qualified Shokuho MissionLogic type for OnAgentBuild hooking.
        /// </summary>
        private const string ShokuhoTypeName =
            "Shokuho.ShokuhoMissions.MissionLogics.ShokuhoAgentEquipmentMissionLogic";

        /// <summary>
        /// Applies Harmony patches to Shokuho's agent equipment logic if the mod is present.
        /// </summary>
        public static void TryPatch(Harmony harmony)
        {
            try
            {
                var type = AccessTools.TypeByName(ShokuhoTypeName);
                if (type == null)
                {
                    Log.Debug(
                        "Shokuho not found; skipping ShokuhoAgentEquipmentMissionLogic patch."
                    );
                    return;
                }

                var original = AccessTools.Method(
                    type,
                    "OnAgentBuild",
                    [typeof(Agent), typeof(Banner)]
                );

                if (original == null)
                {
                    Log.Warning(
                        "ShokuhoAgentEquipmentMissionLogic.OnAgentBuild not found; skipping patch."
                    );
                    return;
                }

                var prefix = new HarmonyMethod(
                    typeof(ShokuhoEquipmentPatcher).GetMethod(
                        nameof(Prefix),
                        BindingFlags.Static | BindingFlags.NonPublic
                    )
                );

                harmony.Patch(original, prefix: prefix);

                Log.Debug("Patched ShokuhoAgentEquipmentMissionLogic.OnAgentBuild.");
            }
            catch (Exception e)
            {
                // Make absolutely sure failure here never crashes game when Shokuho is missing
                Log.Exception(e);
            }
        }

        /// <summary>
        /// Prefix that skips Shokuho's OnAgentBuild for edited troops, returning false to bypass it.
        /// </summary>
        private static bool Prefix(Agent agent, Banner banner)
        {
            try
            {
                if (agent == null || agent.Character == null)
                    return true;

                if (agent.Character is not CharacterObject co)
                    return true;

                var troop = WCharacter.Get(co);

                if (troop.IsEdited)
                    return false; // Skip Shokuho equipment patch for edited troops

                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                // Fail open to avoid weird behavior if something goes wrong
                return true;
            }
        }
    }
}
