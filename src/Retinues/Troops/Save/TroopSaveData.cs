using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Troops.Save
{
    /// <summary>
    /// Serializable save data for a troop, including identity, stats, skills, equipment, and upgrade targets.
    /// </summary>
    public class TroopSaveData
    {
        [SaveableField(1)]
        public string StringId;

        [SaveableField(2)]
        public string VanillaStringId;

        [SaveableField(3)]
        public string Name;

        [SaveableField(4)]
        public int Level;

        [SaveableField(5)]
        public bool IsFemale;

        [SaveableField(6)]
        public string CultureId;

        [SaveableField(7)]
        public List<TroopSaveData> UpgradeTargets = [];

        [SaveableField(8)]
        public TroopEquipmentData EquipmentData;

        [SaveableField(9)]
        public TroopSkillData SkillData;

        [SaveableField(10)]
        public TroopBodySaveData BodyData;

        [SaveableField(11)]
        public int Race;

        [SaveableField(12)]
        public FormationClass FormationClassOverride;

        public TroopSaveData()
        {
            // Default constructor for deserialization
        }

        public TroopSaveData(WCharacter troop)
        {
            if (troop is null)
                return; // Null troop, nothing to do

            StringId = troop.StringId;
            VanillaStringId = troop.VanillaStringId;
            Name = troop.Name;
            Level = troop.Level;
            IsFemale = troop.IsFemale;
            CultureId = troop.Culture.StringId;
            UpgradeTargets = [.. troop.UpgradeTargets.Select(t => new TroopSaveData(t))];
            EquipmentData = new TroopEquipmentData(troop.Loadout.Equipments);
            SkillData = new TroopSkillData(troop.Skills);
            BodyData = Config.EnableTroopCustomization ? new TroopBodySaveData(troop) : null;
            Race = troop.Race;
            FormationClassOverride = troop.FormationClassOverride;
        }

        /// <summary>
        /// Deserializes the troop save data into a WCharacter instance.
        /// </summary>
        public WCharacter Deserialize()
        {
            if (string.IsNullOrEmpty(StringId))
                return null; // Null troop, nothing to do

            // Wrap it
            var troop = new WCharacter(StringId);

            // Get vanilla base
            var vanilla = new WCharacter(VanillaStringId);

            // Fill it
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: false, keepSkills: false);

            // Set properties
            troop.Name = Name;
            troop.Level = Level;
            troop.IsFemale = IsFemale;
            troop.Skills = SkillData.Deserialize();

            // Set equipment
            troop.Loadout.SetEquipments(EquipmentData.Deserialize(troop));

            // Restore upgrade targets
            foreach (var child in UpgradeTargets ?? [])
                troop.AddUpgradeTarget(child.Deserialize());

            // Retinues are not transferable
            if (troop.IsRetinue)
                troop.IsNotTransferableInPartyScreen = true;

            // Set culture visuals if different from vanilla
            if (CultureId != vanilla.Culture.StringId)
            {
                troop.Culture = new WCulture(
                    MBObjectManager.Instance.GetObject<CultureObject>(CultureId)
                );
                BodyPropertyHelper.ApplyPropertiesFromCulture(troop, CultureId);
            }

            // Set race if specified
            if (Race >= 0)
            {
                troop.Race = Race;
                troop.EnsureOwnBodyRange();
            }

            // Set body customization (if enabled)
            if (Config.EnableTroopCustomization)
                BodyData?.Apply(troop);

            // Set formation class override
            troop.FormationClassOverride = FormationClassOverride;

            // Recompute formation class
            troop.FormationClass = troop.ComputeFormationClass();

            // Activate
            troop.Activate();

            if (
                troop?.Name?.ToLowerInvariant().Contains("banner") == true
                && troop?.Name.ToLowerInvariant().Contains("knight") == true
            )
                Log.Info(
                    $"Upgrade Requirement: {troop?.UpgradeItemRequirement?.GetName()} for {troop?.Name}"
                );
            // Return the created troop
            return troop;
        }
    }

    /// <summary>
    /// Serializable save data for a troop, including identity, stats, skills, equipment, and upgrade targets.
    /// </summary>
    public class TroopBodySaveData(WCharacter troop)
    {
        [SaveableField(1)]
        public float AgeMin = troop?.AgeMin ?? 0;

        [SaveableField(2)]
        public float AgeMax = troop?.AgeMax ?? 0;

        [SaveableField(3)]
        public float WeightMin = troop?.WeightMin ?? 0;

        [SaveableField(4)]
        public float WeightMax = troop?.WeightMax ?? 0;

        [SaveableField(5)]
        public float BuildMin = troop?.BuildMin ?? 0;

        [SaveableField(6)]
        public float BuildMax = troop?.BuildMax ?? 0;

        [SaveableField(7)]
        public float HeightMin = troop?.HeightMin ?? 0;

        [SaveableField(8)]
        public float HeightMax = troop?.HeightMax ?? 0;

        // Parameterless constructor for import/export deserialization
        public TroopBodySaveData()
            : this(null) { }

        public void Apply(WCharacter troop)
        {
            // Set dynamic properties (already handles nulls)
            troop.SetDynamicEnd(true, AgeMin, WeightMin, BuildMin);
            troop.SetDynamicEnd(false, AgeMax, WeightMax, BuildMax);
            troop.Age = troop.AgeMin + troop.AgeMax / 2;

            // Set height properties
            if (HeightMin > 0 && HeightMax > 0)
            {
                troop.HeightMin = HeightMin;
                troop.HeightMax = HeightMax;
            }
        }
    }

    /// <summary>
    /// Serializable save data for a troop's skill levels.
    /// </summary>
    public class TroopSkillData(Dictionary<SkillObject, int> skills)
    {
        [SaveableField(1)]
        public string Code = string.Join(";", skills.Select(kv => $"{kv.Key.StringId}:{kv.Value}"));

        // Parameterless constructor for import/export deserialization
        public TroopSkillData()
            : this([]) { }

        public Dictionary<SkillObject, int> Deserialize()
        {
            var result = new Dictionary<SkillObject, int>();
            if (string.IsNullOrWhiteSpace(Code))
                return result;

            var dict = Code.Split(';')
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

    /// <summary>
    /// Serializable save data for a troop's equipment loadout.
    /// </summary>
    public class TroopEquipmentData(List<WEquipment> equipments)
    {
        [SaveableField(1)]
        public List<string> Codes = [.. equipments.Select(we => we.Code)];

        // Per-index civilian flags, null for old saves
        [SaveableField(2)]
        public List<bool> Civilians = [.. equipments.Select(we => we.IsCivilian)];

        // Parameterless constructor for import/export deserialization
        public TroopEquipmentData()
            : this([]) { }

        public List<WEquipment> Deserialize(WCharacter owner)
        {
            var result = new List<WEquipment>(Codes?.Count ?? 0);

            // Back-compat default: if Civilians is missing or size mismatch,
            // use "index 1 is civilian, all others battle".
            bool hasFlags = Civilians != null && Civilians.Count == Codes?.Count;

            for (int idx = 0; idx < (Codes?.Count ?? 0); idx++)
            {
                var code = Codes[idx];

                bool? forceCivilian;

                if (hasFlags)
                    forceCivilian = Civilians[idx];
                else
                    forceCivilian = idx == 1; // legacy default

                // FromCode respects explicit type when provided
                var we = WEquipment.FromCode(
                    code,
                    owner.Loadout,
                    idx,
                    forceCivilian: forceCivilian
                );
                result.Add(we);
            }

            return result;
        }
    }
}
