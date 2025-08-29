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
        public const string IdPrefix = "cct_";
        public const string BasicIdPrefix = IdPrefix + "basic_";
        public const string EliteIdPrefix = IdPrefix + "elite_";

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

        public string Name => GetName();
        public string StringId => GetStringId();

        public int Tier => _characterObject.Tier;
        public int Level => _characterObject.Level;
        public CultureObject Culture => _characterObject.Culture;
        public List<(SkillObject skill, int value)> Skills => GetSkills();
        public List<Equipment> Equipments => GetEquipments();
        public bool IsFemale => GetIsFemale();

        public int Athletics => GetSkill(DefaultSkills.Athletics);
        public int Riding => GetSkill(DefaultSkills.Riding);
        public int OneHanded => GetSkill(DefaultSkills.OneHanded);
        public int TwoHanded => GetSkill(DefaultSkills.TwoHanded);
        public int Polearm => GetSkill(DefaultSkills.Polearm);
        public int Bow => GetSkill(DefaultSkills.Bow);
        public int Crossbow => GetSkill(DefaultSkills.Crossbow);
        public int Throwing => GetSkill(DefaultSkills.Throwing);

        public CharacterObject[] UpgradeTargets { get { return GetUpgradeTargets(); } }

        public ItemCategory UpgradeRequiresItemFromCategory { get { return GetUpgradeRequiresItemFromCategory(); } }

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
                var target = GetUpgradeTargets().FirstOrDefault();

                return target != null ? new CharacterWrapper(target) : null;
            }
        }

        public CharacterWrapper UpgradeTarget2
        {
            get
            {
                var target = GetUpgradeTargets().Skip(1).FirstOrDefault();

                return target != null ? new CharacterWrapper(target) : null;
            }
        }

        public CharacterWrapper(CharacterObject co)
        {
            _characterObject = co;
        }

        public CharacterObject GetCharacterObject()
        {
            return _characterObject;
        }

        public string GetStringId()
        {
            return _characterObject.StringId;
        }

        public void SetName(string name)
        {
            var setNameMethod = Reflector.M((BasicCharacterObject)_characterObject, "SetName", typeof(TextObject));
            setNameMethod.Invoke(_characterObject, new object[] { new TextObject(name, (Dictionary<string, object>)null) });
        }

        public string GetName()
        {
            return _characterObject.Name.ToString();
        }

        public void SetSkills(List<(SkillObject skill, int value)> skills)
        {
            FieldInfo field = Reflector.F<CharacterObject>(_characterObject, "DefaultCharacterSkills");
            field.SetValue(_characterObject, (object)new MBCharacterSkills());

            // Build a dictionary for fast lookup
            var skillDict = skills.ToDictionary(x => x.skill, x => x.value);
            foreach (var skill in TroopSkills)
            {
                int value = skillDict.TryGetValue(skill, out int v) ? v : 0;
                SetSkill(skill, value);
            }
        }

        public List<(SkillObject skill, int value)> GetSkills()
        {
            return TroopSkills.Select(skill => (skill, _characterObject.GetSkillValue(skill))).ToList();
        }

        public void SetSkill(SkillObject skill, int value)
        {
            FieldInfo field = Reflector.F<CharacterObject>(_characterObject, "DefaultCharacterSkills");
            MBCharacterSkills skills = (MBCharacterSkills)field.GetValue(_characterObject);
            ((PropertyOwner<SkillObject>)(object)skills.Skills).SetPropertyValue(skill, value);
        }

        public int GetSkill(SkillObject skill)
        {
            return _characterObject.GetSkillValue(skill);
        }

        public void SetEquipments(List<Equipment> equipments)
        {
            // Prefer the internal setter: it updates EquipmentCode and all caches.
            var m = _characterObject.GetType().GetMethod(
                "SetEquipments",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(Equipment[]) },
                null
            );

            if (m != null)
            {
                m.Invoke(_characterObject, new object[] { equipments.ToArray() });

                // In some builds this is separate; call if it exists.
                var updateCode = _characterObject.GetType().GetMethod("UpdateEquipmentCode",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                updateCode?.Invoke(_characterObject, null);
            }
            else
            {
                // Fallback to your old path if the method isn't present in this version.
                MBEquipmentRoster roster = new MBEquipmentRoster();
                FieldInfo fEquipList = Reflector.F<MBEquipmentRoster>(roster, "_equipments");
                fEquipList.SetValue(roster, new MBList<Equipment>(equipments));
                FieldInfo fRoster = Reflector.F<BasicCharacterObject>((BasicCharacterObject)_characterObject, "_equipmentRoster");
                fRoster.SetValue(_characterObject, roster);
            }

            // Always re-init after changing sets.
            ((BasicCharacterObject)_characterObject).InitializeEquipmentsOnLoad((BasicCharacterObject)_characterObject);
        }


        public List<Equipment> GetEquipments()
        {
            return _characterObject.AllEquipments.ToList();
        }

        public void SetIsFemale(bool isFemale)
        {
            PropertyInfo property = Reflector.P<BasicCharacterObject>(_characterObject, "IsFemale");
            property.SetValue(_characterObject, isFemale);
        }

        public bool GetIsFemale()
        {
            PropertyInfo property = Reflector.P<BasicCharacterObject>(_characterObject, "IsFemale");
            return (bool)property.GetValue(_characterObject);
        }

        public CharacterObject[] GetUpgradeTargets()
        {
            var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeTargets");
            var value = prop.GetValue(_characterObject) as CharacterObject[];
            return value ?? new CharacterObject[0];
        }

        public void AddUpgradeTarget(CharacterWrapper target)
        {
            var oldTargets = UpgradeTargets ?? new TaleWorlds.CampaignSystem.CharacterObject[0];
            var newTargets = new List<TaleWorlds.CampaignSystem.CharacterObject>(oldTargets);
            newTargets.Add(target.GetCharacterObject());
            _characterObject.GetType().GetProperty("UpgradeTargets")?.SetValue(_characterObject, newTargets.ToArray());
        }

        public void SetUpgradeTargets(CharacterObject[] targets)
        {
            var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeTargets");
            prop.SetValue(_characterObject, targets ?? new CharacterObject[0]);
        }

        public ItemCategory GetUpgradeRequiresItemFromCategory()
        {
            var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeRequiresItemFromCategory");
            return (ItemCategory)prop.GetValue(_characterObject);
        }

        public void SetUpgradeRequiresItemFromCategory(ItemCategory value)
        {
            var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeRequiresItemFromCategory");
            prop.SetValue(_characterObject, value);
        }
    }
}
