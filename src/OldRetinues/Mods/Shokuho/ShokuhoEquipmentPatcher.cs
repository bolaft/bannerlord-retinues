using System;
using System.Reflection;
using HarmonyLib;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace OldRetinues.Mods.Shokuho
{
    internal static class ShokuhoEquipmentPatcher
    {
        private const string ShokuhoTypeName =
            "Shokuho.ShokuhoMissions.MissionLogics.ShokuhoAgentEquipmentMissionLogic";

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
                    Log.Debug(
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

                Log.Info("Patched ShokuhoAgentEquipmentMissionLogic.OnAgentBuild.");
            }
            catch (Exception e)
            {
                // Make absolutely sure failure here never crashes game when Shokuho is missing
                Log.Exception(e);
            }
        }

        // If this returns false, Shokuho's OnAgentBuild is skipped.
        private static bool Prefix(Agent agent, Banner banner)
        {
            try
            {
                if (agent == null || agent.Character == null)
                    return true;

                if (agent.Character is not CharacterObject co)
                    return true;

                var troop = new WCharacter(co);

                if (troop.IsCustom || troop.NeedsPersistence)
                    return false; // Skip Shokuho equipment for edited troops

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
