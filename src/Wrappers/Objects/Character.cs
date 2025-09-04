using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Localization;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Utils;
using TaleWorlds.Library;

namespace CustomClanTroops.Wrappers.Objects
{
    public class WCharacter(CharacterObject characterObject, WClan clan = null, WCharacter parent = null) : IWrapper
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly CharacterObject _characterObject = characterObject;

        public object Base => _characterObject;

        // =========================================================================
        // VM properties
        // =========================================================================

        public ImageIdentifierVM Image => new(CharacterCode.CreateFrom(_characterObject));

        public CharacterViewModel Model
        {
            get
            {
                var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                vm.FillFrom(_characterObject, seed: -1);
                return vm;
            }
        }

        // =========================================================================
        // Wrapped properties
        // =========================================================================

        public WCulture Culture => new(_characterObject.Culture);

        public WClan Clan => clan;

        public WCharacter Parent => parent;

        // =========================================================================
        // Basic Attributes
        // =========================================================================

        public string StringId => _characterObject.StringId;

        public string Name
        {
            get => _characterObject.Name.ToString();
            set
            {
                Reflector.InvokeMethod(
                    _characterObject, "SetName", [typeof(TextObject)],
                    new TextObject(value, (System.Collections.Generic.Dictionary<string, object>)null)
                );
            }
        }

        public int Tier => _characterObject.Tier;

        public int Level
        {
            get => _characterObject.Level;
            set => _characterObject.Level = value;
        }

        // =========================================================================
        // Flags & Toggles
        // =========================================================================

        public bool IsElite => Clan.EliteTroops.Contains(this);

        public bool IsFemale
        {
            get => Reflector.GetPropertyValue<bool>(_characterObject, "IsFemale");
            set => Reflector.SetPropertyValue(_characterObject, "IsFemale", value);
        }

        public bool HiddenInEncyclopedia
        {
            get => Reflector.GetPropertyValue<bool>(_characterObject, "HiddenInEncylopedia");
            set => Reflector.SetPropertyValue(_characterObject, "HiddenInEncylopedia", value);
        }

        public bool IsNotTransferableInHideouts
        {
            get => _characterObject.IsNotTransferableInHideouts;
            set => _characterObject.SetTransferableInHideouts(!value);
        }

        public bool IsNotTransferableInPartyScreen
        {
            get => _characterObject.IsNotTransferableInPartyScreen;
            set => _characterObject.SetTransferableInPartyScreen(!value);
        }

        // =========================================================================
        // Skills
        // =========================================================================

        public Dictionary<SkillObject, int> Skills
        {
            get
            {
                return new[]
                {
                    DefaultSkills.Athletics, DefaultSkills.Riding, DefaultSkills.OneHanded, DefaultSkills.TwoHanded,
                    DefaultSkills.Polearm, DefaultSkills.Bow, DefaultSkills.Crossbow, DefaultSkills.Throwing
                }
                .ToDictionary(skill => skill, GetSkill);
            }
            set
            {
                foreach (var skill in value.Keys) SetSkill(skill, value[skill]);
            }
        }

        public int GetSkill(SkillObject skill)
        {
            return _characterObject.GetSkillValue(skill);
        }

        public void SetSkill(SkillObject skill, int value)
        {
            var skills = Reflector.GetFieldValue<MBCharacterSkills>(_characterObject, "DefaultCharacterSkills");
            ((PropertyOwner<SkillObject>)(object)skills.Skills).SetPropertyValue(skill, value);
        }
        public int SkillCap
        {
            get
            {
                return Tier switch
                {
                    1 => 20,
                    2 => 50,
                    3 => 80,
                    4 => 120,
                    5 => 160,
                    6 => 260,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
        }

        public int SkillPoints
        {
            get
            {
                return Tier switch
                {
                    1 => 90,
                    2 => 210,
                    3 => 360,
                    4 => 535,
                    5 => 710,
                    6 => 915,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
        }

        public int SkillPointsUsed => Skills.Sum(skill => GetSkill(skill.Key));

        public int SkillPointsLeft => SkillPoints - SkillPointsUsed;

        public bool CanIncrementSkill(SkillObject skill)
        {
            // Skills can't go above the tier skill cap
            if (GetSkill(skill) >= SkillCap)
                return false;

            // Check if we have enough skill points left
            if (SkillPointsLeft <= 0)
                return false;
            return true;
        }

        public bool CanDecrementSkill(SkillObject skill)
        {
            // Skills can't go below zero
            if (GetSkill(skill) <= 0)
                return false;

            // Check for equipment skill requirements
            if (GetSkill(skill) <= Equipment.GetSkillRequirement(skill))
                return false;

            return true;
        }

        // =========================================================================
        // Equipment
        // =========================================================================

        public List<WEquipment> Equipments
        {
            get
            {
                var equipments = _characterObject.AllEquipments.ToList();
                return [.. equipments.Select(e => new WEquipment(e))];
            }
            set
            {
                var equipments = value.Select(e => (Equipment)e.Base).ToList();
                MBEquipmentRoster roster = new();
                Reflector.SetFieldValue(roster, "_equipments", new MBList<Equipment>(equipments));
                Reflector.SetFieldValue(_characterObject, "_equipmentRoster", roster);
            }
        }

        public WEquipment Equipment => new((Equipment)Equipments.FirstOrDefault().Base);

        public bool CanEquip(WItem item)
        {
            if (item.RelevantSkill != null && item.Difficulty <= GetSkill(item.RelevantSkill))
                return false;  // Does not meet item skill requirements

            return true;
        }

        public void Equip(WItem item, EquipmentIndex slot)
        {
            Equipment.SetItem(slot, item);
        }

        public void Unequip(EquipmentIndex slot)
        {
            Equipment.SetItem(slot, null);
        }

        public void UnequipAll()
        {
            foreach (var slot in WEquipment.Slots)
                Unequip(slot);
        }

        // =========================================================================
        // Upgrades
        // =========================================================================

        public CharacterObject[] UpgradeTargets
        {
            get => Reflector.GetPropertyValue<CharacterObject[]>(_characterObject, "UpgradeTargets") ?? [];
            set => Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", value ?? []);
        }

        public ItemCategory UpgradeRequiresItemFromCategory
        {
            get => Reflector.GetPropertyValue<ItemCategory>(_characterObject, "UpgradeRequiresItemFromCategory");
            set => Reflector.SetPropertyValue(_characterObject, "UpgradeRequiresItemFromCategory", value);
        }

        public void AddUpgradeTarget(WCharacter target)
        {
            var oldTargets = UpgradeTargets ?? [];
            var newTargets = new List<CharacterObject>(oldTargets) { (CharacterObject)target.Base };
            Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", newTargets.ToArray());
        }

        public void RemoveUpgradeTarget(WCharacter target)
        {
            var oldTargets = UpgradeTargets ?? [];
            var newTargets = new List<CharacterObject>(oldTargets) { (CharacterObject)target.Base};
            Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", newTargets.ToArray());
        }

        // =========================================================================
        // Management methods
        // =========================================================================

        public void Register()
        {
            HiddenInEncyclopedia = false;
            IsNotTransferableInPartyScreen = false;
            IsNotTransferableInHideouts = false;
        }

        public void Unregister()
        {
            HiddenInEncyclopedia = true;
            IsNotTransferableInPartyScreen = true;
            IsNotTransferableInHideouts = true;
        }

        public WCharacter Clone(bool keepUpgrades = true, bool keepEquipment = true, bool keepSkills = true)
        {
            // Clone from the source troop
            var cloneObject = CharacterObject.CreateFrom(_characterObject);

            // Wrap it
            WCharacter clone = new(cloneObject, Clan);

            if (keepUpgrades)
                clone.UpgradeTargets = [.. UpgradeTargets];  // Unlink
            else
                clone.UpgradeTargets = [];

            if (keepEquipment)
                clone.Equipments = [.. Equipments];  // Unlink
            else
                clone.Equipments = [];

            if (keepSkills)
                clone.Skills = new Dictionary<SkillObject, int>(Skills);  // Unlink
            else
                clone.Skills = [];

            return clone;
        }
    }
}
