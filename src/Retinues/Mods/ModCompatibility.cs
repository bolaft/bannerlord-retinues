using System.Collections.Generic;
using Retinues.Mods.Shokuho;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Mods
{
    /// <summary>
    /// Handles compatibility checks and integration with other mods.
    /// Registers behaviors for detected mods and warns about known incompatibilities.
    /// </summary>
    [SafeClass]
    public class ModCompatibility : ModuleChecker
    {
        private static readonly List<string> IncompatibleMods =
        [
            // Legacy
            "Retinues.Core",
            "Retinues.MCM",
        ];

        // DLC
        public static bool HasNavalDLC => IsLoaded("NavalDLC");

        // Ruleset flags
        public static bool NoGlobalEditor => IsLoaded("Shokuho");
        public static bool ForceDailyVolunteerSwap => IsLoaded("Shokuho");
        public static bool Tier7Unlocked => IsLoaded("T7TroopUnlocker");
        public static bool SkipItemCultureChecks => IsLoaded("Shokuho", "AD1259");
        public static bool ImprovedGarrisonsHint => IsLoaded("ImprovedGarrisons");
        public static bool PatchShokuhoEquipment => IsLoaded("Shokuho");

        /// <summary>
        /// Adds mod-specific behaviors to the campaign starter if compatible mods are detected.
        /// </summary>
        public static void AddBehaviors(CampaignGameStarter cs)
        {
            // Add mod specific behaviors here
        }

        /// <summary>
        /// Adds Harmony patches for compatible mods.
        /// </summary>
        public static void AddPatches(HarmonyLib.Harmony harmony)
        {
            if (PatchShokuhoEquipment)
                ShokuhoEquipmentPatcher.TryPatch(harmony);
        }

        /// <summary>
        /// Checks for incompatible mods and logs critical warnings if any are found.
        /// </summary>
        public static void IncompatibilityCheck()
        {
            foreach (var modId in IncompatibleMods)
            {
                var mod = GetModule(modId);
                if (mod != null)
                {
                    if (modId.Contains("Retinues."))
                    {
                        Log.Critical(
                            $"[Retinues] WARNING: detected legacy mod '{mod}'. Please uninstall it to avoid conflicts."
                        );
                    }
                    else
                    {
                        Log.Critical($"[Retinues] WARNING: incompatible mod detected: '{mod}'.");
                    }
                }
            }
        }
    }
}
