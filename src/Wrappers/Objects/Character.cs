using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public static IReadOnlyDictionary<int, int> SkillCapsByTier = new Dictionary<int, int>
        {
            { 1, 20 },
            { 2, 50 },
            { 3, 80 },
            { 4, 120 },
            { 5, 160 },
            { 6, 260 }
        };

        public static IReadOnlyDictionary<int, int> TotalSkillPointsByTier = new Dictionary<int, int>
        {
            { 1, 90 },
            { 2, 210 },
            { 3, 360 },
            { 4, 535 },
            { 5, 710 },
            { 6, 915 }
        };

        public static readonly SkillObject[] TroopSkills = new SkillObject[]
        {
            DefaultSkills.Athletics,
            DefaultSkills.Riding,
            DefaultSkills.OneHanded,
            DefaultSkills.TwoHanded,
            DefaultSkills.Polearm,
            DefaultSkills.Bow,
            DefaultSkills.Crossbow,
            DefaultSkills.Throwing
        };

        public string StringId => _characterObject.StringId;

        public int Tier => _characterObject.Tier;

        public CultureObject Culture => _characterObject.Culture;

        public string Name
        {
            get => _characterObject.Name.ToString();
            set
            {
                var setNameMethod = Reflector.M((BasicCharacterObject)_characterObject, "SetName", typeof(TextObject));
                setNameMethod.Invoke(_characterObject, new object[] { new TextObject(value, (Dictionary<string, object>)null) });
            }
        }

        public int Level
        {
            get => _characterObject.Level;
            set => _characterObject.Level = value;
        }

        public List<(SkillObject skill, int value)> Skills
        {
            get => TroopSkills.Select(skill => (skill, _characterObject.GetSkillValue(skill))).ToList();
            set
            {
                FieldInfo field = Reflector.F<CharacterObject>(_characterObject, "DefaultCharacterSkills");
                field.SetValue(_characterObject, (object)new MBCharacterSkills());
                var skillDict = value.ToDictionary(x => x.skill, x => x.value);
                foreach (var skill in TroopSkills)
                {
                    int v = skillDict.TryGetValue(skill, out int val) ? val : 0;
                    SetSkill(skill, v);
                }
            }
        }

        public List<Equipment> Equipments {
            get => _characterObject.AllEquipments.ToList();
            set {
                var m = _characterObject.GetType().GetMethod(
                    "SetEquipments",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Equipment[]) },
                    null
                );
                if (m != null)
                {
                    m.Invoke(_characterObject, new object[] { value.ToArray() });
                    var updateCode = _characterObject.GetType().GetMethod("UpdateEquipmentCode",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    updateCode?.Invoke(_characterObject, null);
                }
                else
                {
                    MBEquipmentRoster roster = new MBEquipmentRoster();
                    FieldInfo fEquipList = Reflector.F<MBEquipmentRoster>(roster, "_equipments");
                    fEquipList.SetValue(roster, new MBList<Equipment>(value));
                    FieldInfo fRoster = Reflector.F<BasicCharacterObject>((BasicCharacterObject)_characterObject, "_equipmentRoster");
                    fRoster.SetValue(_characterObject, roster);
                }
                ((BasicCharacterObject)_characterObject).InitializeEquipmentsOnLoad((BasicCharacterObject)_characterObject);
            }
        }

        public bool IsFemale {
            get {
                PropertyInfo property = Reflector.P<BasicCharacterObject>(_characterObject, "IsFemale");
                return (bool)property.GetValue(_characterObject);
            }
            set {
                PropertyInfo property = Reflector.P<BasicCharacterObject>(_characterObject, "IsFemale");
                property.SetValue(_characterObject, value);
            }
        }

        public int Athletics {
            get => GetSkill(DefaultSkills.Athletics);
            set => SetSkill(DefaultSkills.Athletics, value);
        }
        public int Riding {
            get => GetSkill(DefaultSkills.Riding);
            set => SetSkill(DefaultSkills.Riding, value);
        }
        public int OneHanded {
            get => GetSkill(DefaultSkills.OneHanded);
            set => SetSkill(DefaultSkills.OneHanded, value);
        }
        public int TwoHanded {
            get => GetSkill(DefaultSkills.TwoHanded);
            set => SetSkill(DefaultSkills.TwoHanded, value);
        }
        public int Polearm {
            get => GetSkill(DefaultSkills.Polearm);
            set => SetSkill(DefaultSkills.Polearm, value);
        }
        public int Bow {
            get => GetSkill(DefaultSkills.Bow);
            set => SetSkill(DefaultSkills.Bow, value);
        }
        public int Crossbow {
            get => GetSkill(DefaultSkills.Crossbow);
            set => SetSkill(DefaultSkills.Crossbow, value);
        }
        public int Throwing {
            get => GetSkill(DefaultSkills.Throwing);
            set => SetSkill(DefaultSkills.Throwing, value);
        }

        public CharacterObject[] UpgradeTargets {
            get {
                var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeTargets");
                var value = prop.GetValue(_characterObject) as CharacterObject[];
                return value ?? new CharacterObject[0];
            }
            set {
                var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeTargets");
                prop.SetValue(_characterObject, value ?? new CharacterObject[0]);
            }
        }

        public ItemCategory UpgradeRequiresItemFromCategory {
            get {
                var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeRequiresItemFromCategory");
                return (ItemCategory)prop.GetValue(_characterObject);
            }
            set {
                var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeRequiresItemFromCategory");
                prop.SetValue(_characterObject, value);
            }
        }

        private CharacterObject _characterObject;

        public CharacterViewModel ViewModel
        {
            get
            {
                var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                vm.FillFrom(_characterObject, seed: -1);
                return vm;
            }
        }

        public CharacterWrapper UpgradeTarget1
        {
            get
            {
                var target = UpgradeTargets.FirstOrDefault();

                return target != null ? new CharacterWrapper(target) : null;
            }
        }

        public CharacterWrapper UpgradeTarget2
        {
            get
            {
                var target = UpgradeTargets.Skip(1).FirstOrDefault();

                return target != null ? new CharacterWrapper(target) : null;
            }
        }

        public CharacterWrapper(CharacterObject co) => _characterObject = co;

        public CharacterObject GetCharacterObject()
        {
            return _characterObject;
        }

        public int GetSkill(SkillObject skill)
        {
            return _characterObject.GetSkillValue(skill);
        }

        public void SetSkill(SkillObject skill, int value)
        {
            FieldInfo field = Reflector.F<CharacterObject>(_characterObject, "DefaultCharacterSkills");
            MBCharacterSkills skills = (MBCharacterSkills)field.GetValue(_characterObject);
            ((PropertyOwner<SkillObject>)(object)skills.Skills).SetPropertyValue(skill, value);
        }

        public void AddUpgradeTarget(CharacterWrapper target)
        {
            var oldTargets = UpgradeTargets ?? new TaleWorlds.CampaignSystem.CharacterObject[0];
            var newTargets = new List<TaleWorlds.CampaignSystem.CharacterObject>(oldTargets);
            newTargets.Add(target.GetCharacterObject());
            _characterObject.GetType().GetProperty("UpgradeTargets")?.SetValue(_characterObject, newTargets.ToArray());
        }
    }
}
