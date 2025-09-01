using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using CustomClanTroops.Utils;
using CustomClanTroops.Wrappers.Campaign;

namespace CustomClanTroops.Wrappers.Objects
{
    public class CharacterWrapper
    {
        // =========================================================================
        // Fields & Construction
        // =========================================================================

        private CharacterObject _characterObject;

        public CharacterWrapper(CharacterObject co)
        {
            _characterObject = co;
        }

        // =========================================================================
        // Identity & Core Accessors
        // =========================================================================

        public CharacterObject CharacterObject => _characterObject;

        public string StringId => _characterObject.StringId;

        public int Tier => _characterObject.Tier;

        public CultureObject Culture => _characterObject.Culture;

        public string Name
        {
            get => _characterObject.Name.ToString();
            set
            {
                Reflector.InvokeMethod(
                    _characterObject,
                    "SetName",
                    new[] { typeof(TextObject) },
                    new TextObject(value, (System.Collections.Generic.Dictionary<string, object>)null));
            }
        }

        public int Level
        {
            get => _characterObject.Level;
            set => _characterObject.Level = value;
        }

        // =========================================================================
        // Relationships
        // =========================================================================

        public CharacterWrapper Parent;

        // =========================================================================
        // Equipment Management
        // =========================================================================

        public List<Equipment> Equipments
        {
            get => _characterObject.AllEquipments.ToList();
            set
            {
                try
                {
                    Reflector.InvokeMethod(_characterObject, "SetEquipments", new[] { typeof(Equipment[]) }, value.ToArray());
                    Reflector.InvokeMethod(_characterObject, "UpdateEquipmentCode", Type.EmptyTypes, Array.Empty<object>());
                }
                catch (MissingMethodException)
                {
                    // Fallback: write fields directly
                    MBEquipmentRoster roster = new MBEquipmentRoster();
                    Reflector.SetFieldValue(roster, "_equipments", new MBList<Equipment>(value));
                    Reflector.SetFieldValue(_characterObject, "_equipmentRoster", roster);
                }

                ((BasicCharacterObject)_characterObject).InitializeEquipmentsOnLoad((BasicCharacterObject)_characterObject);
            }
        }

        // =========================================================================
        // Flags & Gameplay Toggles
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
        // Upgrade Data (properties)
        // =========================================================================

        public CharacterObject[] UpgradeTargets
        {
            get => Reflector.GetPropertyValue<CharacterObject[]>(_characterObject, "UpgradeTargets") ?? Array.Empty<CharacterObject>();
            set => Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", value ?? Array.Empty<CharacterObject>());
        }

        public ItemCategory UpgradeRequiresItemFromCategory
        {
            get => Reflector.GetPropertyValue<ItemCategory>(_characterObject, "UpgradeRequiresItemFromCategory");
            set => Reflector.SetPropertyValue(_characterObject, "UpgradeRequiresItemFromCategory", value);
        }

        // =========================================================================
        // View Model
        // =========================================================================

        public CharacterViewModel ViewModel
        {
            get
            {
                var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                vm.FillFrom(_characterObject, seed: -1);
                return vm;
            }
        }

        // =========================================================================
        // Skills (caps, totals, helpers)
        // =========================================================================

        public int SkillCap
        {
            get
            {
                return Tier switch
                {
                    1 => 20, 2 => 50, 3 => 80, 4 => 120, 5 => 160, 6 => 260,
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
                    1 => 90, 2 => 210, 3 => 360, 4 => 535, 5 => 710, 6 => 915,
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }
        }

        public Dictionary<SkillObject, int> Skills =>
            new[]
            {
                DefaultSkills.Athletics,
                DefaultSkills.Riding,
                DefaultSkills.OneHanded,
                DefaultSkills.TwoHanded,
                DefaultSkills.Polearm,
                DefaultSkills.Bow,
                DefaultSkills.Crossbow,
                DefaultSkills.Throwing
            }
            .ToDictionary(skill => skill, skill => GetSkill(skill));

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
        // Upgrade Helpers (methods)
        // =========================================================================

        public void AddUpgradeTarget(CharacterWrapper target)
        {
            var oldTargets = UpgradeTargets ?? Array.Empty<CharacterObject>();
            var newTargets = new List<CharacterObject>(oldTargets) { target.CharacterObject };
            Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", newTargets.ToArray());
        }

        public void RemoveUpgradeTarget(CharacterWrapper target)
        {
            var oldTargets = UpgradeTargets ?? Array.Empty<CharacterObject>();
            var newTargets = new List<CharacterObject>(oldTargets);
            newTargets.Remove(target.CharacterObject);
            Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", newTargets.ToArray());
        }

        // =========================================================================
        // Cloning
        // =========================================================================

        public CharacterWrapper Clone()
        {
            // Clone from the source troop
            var cloneObj = CharacterObject.CreateFrom(_characterObject);

            // Wrap it
            CharacterWrapper clone = new CharacterWrapper(cloneObj);

            // Set UpgradeTargets
            clone.UpgradeTargets = Array.Empty<CharacterObject>();

            Log.Debug($"[CharacterHelpers] Cloned troop '{_characterObject.StringId}' to '{cloneObj.StringId}'.");

            return clone;
        }
    }
}
