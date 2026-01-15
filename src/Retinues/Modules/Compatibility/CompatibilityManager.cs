using System.Collections.Generic;
using HarmonyLib;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Modules.Compatibility
{
    /// <summary>
    /// Manages interoperations between Retinues and other modules (mods, DLC).
    /// Handles detection, compatibility flags, and hooks for patches/behaviors.
    /// </summary>
    [SafeClass]
    public static class CompatibilityManager
    {
        private static readonly List<string> IncompatibleMods = [];

        /// <summary>
        /// Adds mod-specific behaviors to the campaign starter if compatible mods are detected.
        /// </summary>
        public static void RegisterCompatibilityBehaviors(CampaignGameStarter cs)
        {
            // Currently no compatibility behaviors to register.
        }

        /// <summary>
        /// Adds Harmony patches for compatible mods.
        /// </summary>
        public static void AddPatches(Harmony harmony)
        {
            if (Mods.Shokuho.IsLoaded)
                Shokuho.ShokuhoEquipmentPatcher.TryPatch(harmony);

            if (Mods.BanditMilitias.IsLoaded)
                BanditMilitias.BanditMilitiasTroopsPatcher.TryPatch(harmony);
        }

        /// <summary>
        /// Checks for incompatible mods and logs critical warnings if any are found.
        /// </summary>
        public static void CheckIncompatibilities()
        {
            foreach (var modId in IncompatibleMods)
            {
                var mod = ModuleManager.GetModule(modId);
                if (mod != null)
                {
                    Log.Critical($"WARNING: incompatible mod detected: '{mod}'.");
                }
            }
        }
    }
}
