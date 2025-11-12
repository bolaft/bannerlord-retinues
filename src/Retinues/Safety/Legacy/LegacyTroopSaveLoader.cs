using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Safety.Legacy
{
    /// <summary>
    /// Static helpers for loading legacy custom troop data.
    /// </summary>
    [SafeClass]
    public static class LegacyTroopSaveLoader
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Loading                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly Dictionary<string, string> TroopIdMap = [];

        /// <summary>
        /// Loads a WCharacter from LegacyTroopSaveData, recursively restoring upgrades and properties.
        /// </summary>
        public static WCharacter Load(LegacyTroopSaveData data)
        {
            // Wrap it
            var troop = new WCharacter();

            // Map legacy ID to new ID
            TroopIdMap[data.StringId] = troop.StringId;

            // Get vanilla base
            var vanilla = new WCharacter(data.VanillaStringId);

            // Fill it
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: false, keepSkills: false);

            // Set properties
            troop.Name = data.Name;
            troop.Level = data.Level;
            troop.IsFemale = data.IsFemale;
            troop.Skills = SkillsFromCode(data.SkillCode);

            if (data.EquipmentCode != null)
            {
                // Legacy support for single equipment code
                Log.Warn(
                    "TroopSaveData.EquipmentCode is deprecated, use EquipmentCodes list instead."
                );
                troop.Loadout.SetEquipments(
                    [
                        WEquipment.FromCode(
                            data.EquipmentCode,
                            troop.Loadout,
                            (int)EquipmentCategory.Battle
                        ),
                        WEquipment.FromCode(
                            data.EquipmentCode,
                            troop.Loadout,
                            (int)EquipmentCategory.Civilian
                        ),
                    ]
                );
            }
            else
            {
                if (troop.Loadout.Equipments.Count == 0)
                {
                    // create two empty equipments if none exist
                    troop.Loadout.Clear();
                }
                else if (troop.Loadout.Equipments.Count == 1)
                {
                    // ensure we have a second equipment for civilian
                    troop.Loadout.SetEquipments(
                        [
                            troop.Loadout.Equipments[0],
                            WEquipment.FromCode(
                                null,
                                troop.Loadout,
                                (int)EquipmentCategory.Civilian
                            ),
                        ]
                    );
                }
                else
                {
                    // set correct flags
                    troop.Loadout.SetEquipments(
                        [
                            .. data.EquipmentCodes.Select(
                                (code, idx) => WEquipment.FromCode(code, troop.Loadout, idx)
                            ),
                        ]
                    );
                }
            }

            // Activate before loading children so ID is allocated
            troop.Activate();

            // Restore upgrade targets
            foreach (var child in data.UpgradeTargets ?? [])
                troop.AddUpgradeTarget(Load(child));

            // Retinues are not transferable
            if (troop.IsRetinue)
                troop.IsNotTransferableInPartyScreen = true;

            // Set culture visuals if present
            if (!string.IsNullOrEmpty(data.CultureId) && data.CultureId != vanilla.Culture.StringId)
            {
                var culture = MBObjectManager.Instance.GetObject<CultureObject>(data.CultureId);
                if (culture != null)
                {
                    Log.Info($"Applying culture '{data.CultureId}' to troop '{data.StringId}'");
                    troop.Culture = new WCulture(culture);
                    BodyPropertyHelper.ApplyPropertiesFromCulture(troop, culture);
                }
            }

            if (data.Race >= 0)
            {
                troop.Race = data.Race;
                troop.EnsureOwnBodyRange();
            }

            if (Config.EnableTroopCustomization)
            {
                // Set dynamic properties (already handles nulls)
                troop.SetDynamicEnd(true, data.AgeMin, data.WeightMin, data.BuildMin);
                troop.SetDynamicEnd(false, data.AgeMax, data.WeightMax, data.BuildMax);
                troop.Age = troop.AgeMin + troop.AgeMax / 2;

                // Set height properties
                if (data.HeightMin > 0 && data.HeightMax > 0)
                {
                    troop.HeightMin = data.HeightMin;
                    troop.HeightMax = data.HeightMax;
                }
            }

            // Return the created troop
            return troop;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Parses a skill code string into a skill dictionary for loading.
        /// </summary>
        public static Dictionary<SkillObject, int> SkillsFromCode(string skillsString)
        {
            var result = new Dictionary<SkillObject, int>();
            if (string.IsNullOrWhiteSpace(skillsString))
                return result;

            var dict = skillsString
                .Split(';')
                .Select(part => part.Split(':'))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => int.Parse(parts[1]));

            foreach (var kv in dict)
            {
                var skill = MBObjectManager.Instance.GetObject<SkillObject>(kv.Key);
                if (skill != null)
                    result[skill] = kv.Value;
            }

            return result;
        }
    }
}
