using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Features.Experience;
using Retinues.Features.Staging;
using Retinues.Game;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using Retinues.Safety.Legacy;
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
        public FormationClass FormationClassOverride = FormationClass.Unset;

        [SaveableField(13)]
        public TroopSaveData Captain;

        [SaveableField(14)]
        public bool IsCaptain;

        [SaveableField(15)]
        public bool CaptainEnabled = false;

        [SaveableField(16)]
        public bool IsMariner = false;

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
            BodyData = Config.EnableTroopCustomization ? new TroopBodySaveData(troop.Body) : null;
            Race = troop.Race;
            FormationClassOverride = troop.FormationClassOverride;
            IsCaptain = troop.IsCaptain;
            CaptainEnabled = troop.CaptainEnabled;
            IsMariner = troop.IsMariner;

            // For captains, Captain will stay null to avoid recursion.
            if (!troop.IsCaptain && troop.Captain != null)
                Captain = new TroopSaveData(troop.Captain);
        }

        /// <summary>
        /// Checks if the troop save data is valid (has a StringId).
        /// </summary>
        private bool IsValid() => !string.IsNullOrEmpty(StringId);

        /// <summary>
        /// Creates a WCharacter instance based on the available context.
        /// </summary>
        private WCharacter MakeTroop(
            string stringId = null,
            WFaction faction = null,
            RootCategory category = RootCategory.Other,
            WCharacter parent = null
        )
        {
            if (!IsValid())
                return null; // Null troop, nothing to do

            if (faction != null && category != RootCategory.Other)
                return new WCharacter(faction, category, stringId ?? StringId);
            if (parent != null)
                return new WCharacter(parent, stringId ?? StringId);
            if (stringId != null)
                return new WCharacter(stringId);

            return null;
        }

        /// <summary>
        /// Deserializes the troop save data into a WCharacter instance.
        /// Used when the faction context is not needed (culture troops).
        /// </summary>
        public WCharacter Deserialize()
        {
            var troop = MakeTroop(stringId: StringId);

            return DeserializeInternal(troop);
        }

        /// <summary>
        /// Deserializes the troop save data into a WCharacter instance.
        /// Used when the faction context is needed (faction roots).
        /// </summary>
        public WCharacter Deserialize(WFaction faction, RootCategory category)
        {
            var troop = MakeTroop(stringId: StringId, faction: faction, category: category);

            return DeserializeInternal(troop);
        }

        /// <summary>
        /// Deserializes the troop save data into a WCharacter instance.
        /// Used when the parent troop context is needed (upgrade targets).
        /// </summary>
        public WCharacter Deserialize(WCharacter parent)
        {
            var troop = MakeTroop(stringId: StringId, parent: parent);

            return DeserializeInternal(troop);
        }

        /// <summary>
        /// Deserializes the troop save data into a WCharacter instance.
        /// </summary>
        public WCharacter DeserializeInternal(WCharacter troop)
        {
            if (troop == null)
                return null; // Null troop, nothing to do

            // If this is a vanilla troop, keep it marked as edited so it continues to be saved
            if (troop.IsVanilla)
                troop.NeedsPersistence = true; // Loaded from save, must be persisted again

            // Get vanilla base
            var vanilla = new WCharacter(VanillaStringId);

            // Fill it
            troop.FillFrom(
                vanilla,
                keepUpgrades: troop.IsVanilla,
                keepEquipment: false,
                keepSkills: false
            );

            // Set properties
            troop.Name = Name;
            troop.Level = Level;
            troop.IsFemale = IsFemale;
            troop.Skills = SkillData.Deserialize();

            // Set equipment
            troop.Loadout.SetEquipments(EquipmentData.Deserialize(troop));

            // Restore upgrade targets
            foreach (var child in UpgradeTargets ?? [])
                child.Deserialize(troop); // Custom path (Parent)

            // Set culture visuals if different from vanilla
            if (CultureId != vanilla.Culture.StringId)
            {
                troop.Culture = new WCulture(
                    MBObjectManager.Instance.GetObject<CultureObject>(CultureId)
                );
                BodyHelper.ApplyPropertiesFromCulture(troop, CultureId);
            }

            // Set race if specified
            if (Race >= 0)
            {
                troop.Race = Race;
                troop.Body.EnsureOwnBodyRange();
            }

            // Set body customization (if enabled)
            if (Config.EnableTroopCustomization)
                BodyData?.Apply(troop.Body);

            // Set formation class override
            troop.FormationClassOverride = FormationClassOverride;

            // Recompute formation class, upgrade requirements, etc.
            troop.ComputeDerivedProperties();

            // If this is a legacy custom troop, perform special handling
            try
            {
                if (StringId.StartsWith(WCharacter.LegacyCustomIdPrefix))
                {
                    var legacyTroop = WCharacter.FromStringId(StringId);

                    // Fill from for those that sip back somehow, at least they'll look right
                    legacyTroop.FillFrom(troop);

                    // Replace any existing troop with the same StringId
                    legacyTroop.Replace(troop);

                    // Migrate XP pool from old key to new key
                    TroopXpBehavior.ReplacePoolKey(StringId, troop.StringId);

                    // Migrate staged changes from old key to new key
                    BaseUpgradeBehavior<PendingEquipData>.ReplacePendingKey(
                        StringId,
                        troop.StringId
                    );
                    BaseUpgradeBehavior<PendingTrainData>.ReplacePendingKey(
                        StringId,
                        troop.StringId
                    );
                }
            }
            catch (System.Exception e)
            {
                Log.Exception(e);
            }

            // Fix face tags
            BodyHelper.ApplyTagsFromCulture(troop);

            // Captain spawn toggle (default false)
            troop.CaptainEnabled = CaptainEnabled;

            // Rebuild captain if present in save data (and this troop is not itself a captain)
            try
            {
                if (!IsCaptain && Captain != null)
                {
                    // Deserialize captain as a standalone custom troop
                    var captain = Captain.Deserialize();

                    if (captain != null)
                    {
                        // Share faction and transfer flags
                        if (troop.Faction != null)
                            captain.Faction = troop.Faction;

                        // Bind relationship (sets IsCaptain, BaseTroop, ActiveStubIds, flags)
                        troop.BindCaptain(captain);
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.Exception(e);
            }

            // Mariner status
            troop.IsMariner = IsMariner;

            // Return the created troop
            return troop;
        }
    }

    /// <summary>
    /// Serializable save data for a troop, including identity, stats, skills, equipment, and upgrade targets.
    /// </summary>
    public class TroopBodySaveData(WBody body)
    {
        [SaveableField(1)]
        public float AgeMin = body?.AgeMin ?? 0;

        [SaveableField(2)]
        public float AgeMax = body?.AgeMax ?? 0;

        [SaveableField(3)]
        public float WeightMin = body?.WeightMin ?? 0;

        [SaveableField(4)]
        public float WeightMax = body?.WeightMax ?? 0;

        [SaveableField(5)]
        public float BuildMin = body?.BuildMin ?? 0;

        [SaveableField(6)]
        public float BuildMax = body?.BuildMax ?? 0;

        [SaveableField(7)]
        public float HeightMin = body?.HeightMin ?? 0;

        [SaveableField(8)]
        public float HeightMax = body?.HeightMax ?? 0;

        // Parameterless constructor for import/export deserialization
        public TroopBodySaveData()
            : this(null) { }

        public void Apply(WBody body)
        {
            // Set dynamic properties (already handles nulls)
            body.SetDynamicEnd(true, AgeMin, WeightMin, BuildMin);
            body.SetDynamicEnd(false, AgeMax, WeightMax, BuildMax);
            body.Age = body.AgeMin + body.AgeMax / 2;

            // Set height properties
            if (HeightMin > 0 && HeightMax > 0)
            {
                body.HeightMin = HeightMin;
                body.HeightMax = HeightMax;
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
