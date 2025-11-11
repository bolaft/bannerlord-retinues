using System;
using HarmonyLib;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Safety.Patches
{
    [HarmonyPatch(typeof(Agent), "GetProtectorArmorMaterialOfBone")]
    internal static class Agent_GetProtectorArmorMaterialOfBone_Patch
    {
        // Return false to short-circuit vanilla when we detect a bad armor item.
        static bool Prefix(
            Agent __instance,
            sbyte boneIndex,
            ref ArmorComponent.ArmorMaterialTypes __result
        )
        {
            try
            {
                if (boneIndex < 0 || __instance == null)
                    return true;

                var visuals = __instance.AgentVisuals;
                if (visuals == null)
                    return true;

                EquipmentIndex idx = EquipmentIndex.None;
                switch (visuals.GetBoneTypeData(boneIndex).BodyPartType)
                {
                    case BoneBodyPartType.Chest:
                    case BoneBodyPartType.Abdomen:
                    case BoneBodyPartType.ShoulderLeft:
                    case BoneBodyPartType.ShoulderRight:
                        idx = EquipmentIndex.Body;
                        break;
                    case BoneBodyPartType.ArmLeft:
                    case BoneBodyPartType.ArmRight:
                        idx = EquipmentIndex.Gloves;
                        break;
                    case BoneBodyPartType.Legs:
                        idx = EquipmentIndex.Leg;
                        break;
                    case BoneBodyPartType.Head:
                    case BoneBodyPartType.Neck:
                        // Head armor slot is exactly NumAllWeaponSlots in this version.
                        idx = EquipmentIndex.NumAllWeaponSlots;
                        break;
                }

                if (idx != EquipmentIndex.None)
                {
                    var el = __instance.SpawnEquipment[idx];
                    var item = el.Item;
                    // BUGGUARD: vanilla dereferences ArmorComponent without checking null.
                    if (item != null && item.ArmorComponent == null)
                    {
                        Log.Warn(
                            $"[ArmorGuard] {__instance?.Character?.StringId} has non-armor item in {idx}: {item.StringId}. Treating as Armor=None."
                        );
                        __result = ArmorComponent.ArmorMaterialTypes.None;
                        return false; // skip vanilla to avoid NRE
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"[ArmorGuard] Exception in Prefix: {e}");
                return true; // fall back to vanilla on error
            }

            return true; // run vanilla normally
        }
    }
}
