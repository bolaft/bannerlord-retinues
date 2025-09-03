using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Localization;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Wrappers.Objects
{
    public class CharacterWrapper(CharacterObject characterObject)
    {
        // =========================================================================
        // Base
        // =========================================================================

        protected readonly CharacterObject _characterObject = characterObject;

        public CharacterObject Base => _characterObject;

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

        public CultureWrapper Culture => new(_characterObject.Culture);

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

        // =========================================================================
        // Equipment
        // =========================================================================

        public List<EquipmentWrapper> Equipments
        {
            get => [.. _characterObject.AllEquipments.Select(e => new EquipmentWrapper(e))];
            set
            {
                // Convert the list of EquipmentWrapper to an array of Equipment objects
                var equipmentArray = value.Select(w => w.Base).ToArray();
                // Set the character's equipments using reflection
                Reflector.InvokeMethod(_characterObject, "SetEquipments", [typeof(Equipment[])], equipmentArray);
                // Update the equipment code to reflect changes
                Reflector.InvokeMethod(_characterObject, "UpdateEquipmentCode", Type.EmptyTypes, []);
                // Re-initialize the character's equipment state
                _characterObject.InitializeEquipmentsOnLoad(_characterObject);
            }
        }

        // =========================================================================
        // Upgrades
        // =========================================================================

        public List<CharacterObject> UpgradeTargets
        {
            get => Reflector.GetPropertyValue<List<CharacterObject>>(_characterObject, "UpgradeTargets") ?? new List<CharacterObject>();
            set => Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", value ?? new List<CharacterObject>());
        }

        public ItemCategory UpgradeRequiresItemFromCategory
        {
            get => Reflector.GetPropertyValue<ItemCategory>(_characterObject, "UpgradeRequiresItemFromCategory");
            set => Reflector.SetPropertyValue(_characterObject, "UpgradeRequiresItemFromCategory", value);
        }
    }
}
