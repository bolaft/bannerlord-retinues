using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using CustomClanTroops.Wrappers.Campaign;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Wrappers.Objects
{
    public class WCharacter(CharacterObject characterObject, WFaction faction = null, WCharacter parent = null) : StringIdentifier, IWrapper
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

                if (Faction != null)
                {
                    // For armor colors
                    vm.ArmorColor1 = Faction.Color;
                    vm.ArmorColor2 = Faction.Color2;

                    // For heraldic items
                    vm.BannerCodeText = Faction.BannerCodeText;
                }

                return vm;
            }
        }

        // =========================================================================
        // Wrapped properties
        // =========================================================================

        public WCulture Culture => new(_characterObject.Culture);

        public WFaction Faction => faction;

        public WCharacter Parent => parent;

        // =========================================================================
        // Basic Attributes
        // =========================================================================

        public override string StringId => _characterObject.StringId;

        public string Name
        {
            get => _characterObject.Name.ToString();
            set
            {
                Reflector.InvokeMethod(
                    _characterObject, "SetName", [typeof(TextObject)],
                    new TextObject(value, (Dictionary<string, object>)null)
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

        public bool IsElite => Faction.EliteTroops.Contains(this);

        public bool IsMaxTier => Tier >= (IsElite ? 6 : 5);

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
            if (item.RelevantSkill == null)
                return true; // No requirements
            
            if (item.Difficulty <= GetSkill(item.RelevantSkill))
                return true; // Meets item skill requirements

            return false;
        }

        public void Equip(WItem item, EquipmentIndex slot)
        {
            Equipment.SetItem(slot, item);
        }

        public WItem Unequip(EquipmentIndex slot)
        {
            var item = Equipment.GetItem(slot);
            Equipment.SetItem(slot, null);
            return item;
        }

        public IEnumerable<WItem> UnequipAll()
        {
            foreach (var slot in WEquipment.Slots)
                yield return Unequip(slot);
        }

        // =========================================================================
        // Upgrades
        // =========================================================================

        public WCharacter[] UpgradeTargets
        {
            get
            {
                var arr = Reflector.GetPropertyValue<CharacterObject[]>(_characterObject, "UpgradeTargets") ?? [];
                return [.. arr.Select(obj => new WCharacter(obj, Faction, this))];
            }
            set
            {
                var arr = value?.Select(wc => (CharacterObject)wc.Base).ToArray() ?? [];
                Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", arr);
            }
        }

        public ItemCategory UpgradeRequiresItemFromCategory
        {
            get => Reflector.GetPropertyValue<ItemCategory>(_characterObject, "UpgradeRequiresItemFromCategory");
            set => Reflector.SetPropertyValue(_characterObject, "UpgradeRequiresItemFromCategory", value);
        }

        public void AddUpgradeTarget(WCharacter target)
        {
            var oldTargets = UpgradeTargets ?? [];
            var newTargets = new List<WCharacter>(oldTargets) { target };
            Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", newTargets.Select(wc => (CharacterObject)wc.Base).ToArray());
        }

        public void RemoveUpgradeTarget(WCharacter target)
        {
            var oldTargets = UpgradeTargets ?? [];
            var newTargets = new List<WCharacter>(oldTargets);
            newTargets.RemoveAll(wc => wc.Base == target.Base);
            Reflector.SetPropertyValue(_characterObject, "UpgradeTargets", newTargets.Select(wc => (CharacterObject)wc.Base).ToArray());
        }

        // =========================================================================
        // Management methods
        // =========================================================================

        public void Remove()
        {
            // Remove from faction lists
            if (Faction != null)
            {
                if (IsElite)
                    Faction.EliteTroops.Remove(this);
                else
                    Faction.BasicTroops.Remove(this);
            }

            // Remove from parent's upgrade targets
            Parent?.RemoveUpgradeTarget(this);

            Log.Debug($"Removed troop {Name} from parent {Parent?.Name ?? "null"} and faction {Faction?.Name ?? "null"}");

            // Unregister from the game systems
            Unregister();

            // Remove all children
            foreach (var target in UpgradeTargets)
                target.Remove();
        }

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

        public WCharacter Clone(WFaction faction = null, WCharacter parent = null, bool keepUpgrades = true, bool keepEquipment = true, bool keepSkills = true)
        {
            // Clone from the source troop
            var cloneObject = CharacterObject.CreateFrom(_characterObject);

            // Default faction is the same as the original troop
            faction ??= Faction;

            // Wrap it
            WCharacter clone = new(cloneObject, faction, parent);

            if (keepUpgrades)
                clone.UpgradeTargets = [.. UpgradeTargets]; // Unlink
            else
                clone.UpgradeTargets = [];

            if (keepEquipment)
                clone.Equipments = [.. Equipments]; // Unlink
            else
                clone.Equipments = [];

            if (keepSkills)
                clone.Skills = new Dictionary<SkillObject, int>(Skills); // Unlink
            else
                clone.Skills = [];

            return clone;
        }
    }
}
