using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
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
            BodyData = new TroopBodySaveData(troop);
            Race = troop.Race;
            FormationClassOverride = troop.FormationClassOverride;
        }

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
            troop.Loadout.Equipments = EquipmentData.Deserialize(troop);

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
                CharacterCustomization.ApplyPropertiesFromCulture(troop, CultureId);
            }

            // Set race if specified
            if (Race >= 0)
            {
                troop.Race = Race;
                troop.EnsureOwnBodyRange();
            }

            // Set body customization (if enabled)
            if (Config.EnableTroopCustomization)
                BodyData.Apply(troop);

            // Set formation class override
            troop.FormationClassOverride = FormationClassOverride;

            // Recompute formation class
            troop.FormationClass = troop.ComputeFormationClass();

            // Activate
            troop.Activate();

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
        public float AgeMin = troop.AgeMin;

        [SaveableField(2)]
        public float AgeMax = troop.AgeMax;

        [SaveableField(3)]
        public float WeightMin = troop.WeightMin;

        [SaveableField(4)]
        public float WeightMax = troop.WeightMax;

        [SaveableField(5)]
        public float BuildMin = troop.BuildMin;

        [SaveableField(6)]
        public float BuildMax = troop.BuildMax;

        [SaveableField(7)]
        public float HeightMin = troop.HeightMin;

        [SaveableField(8)]
        public float HeightMax = troop.HeightMax;

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

        public List<WEquipment> Deserialize(WCharacter owner)
        {
            return [.. Codes.Select((code, idx) => WEquipment.FromCode(code, owner.Loadout, idx))];
        }
    }
}
