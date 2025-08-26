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

        private const Occupation DefaultOccupation = Occupation.Soldier;

        public string Name => GetName();
        public string StringId => GetStringId();
        public int Tier => _characterObject.Tier;
        public int Level => _characterObject.Level;
        public Occupation Occupation => GetOccupation();
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

        public CharacterObject BaseCharacter => _characterObject;

        private CharacterViewModel _viewModel;
        public CharacterViewModel ViewModel
        {
            get
            {
                if (_viewModel == null)
                {
                    // TODO: Fix this, doesn't work, using default base unit instead

                    // _viewModel = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                    // _viewModel.FillFrom(_characterObject, seed: -1);
                    // Log.Info($"CharacterWrapper: Created ViewModel for character {_characterObject.StringId}");
                    // Log.Info($"CharacterWrapper: ViewModel details - CharStringId: {_viewModel.CharStringId}, Race: {_viewModel.Race}, EquipmentCode: {_viewModel.EquipmentCode}");

                    Log.Warn($"CharacterWrapper: ViewModel not created for character {_characterObject.StringId}, using default.");

                    HeroWrapper hero = new HeroWrapper();
                    CharacterWrapper baseCultureTroop = hero.Culture.RootBasic;

                    _viewModel = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                    _viewModel.FillFrom(baseCultureTroop.BaseCharacter, seed: -1);
                    Log.Info($"CharacterWrapper: Created ViewModel for character {baseCultureTroop.StringId}");
                    Log.Info($"CharacterWrapper: ViewModel details - CharStringId: {_viewModel.CharStringId}, Race: {_viewModel.Race}, EquipmentCode: {_viewModel.EquipmentCode}");
                }
                return _viewModel;
            }
        }

        // Constructor: from existing CharacterObject
        public CharacterWrapper(CharacterObject co)
        {
            _characterObject = co;

        }

        // Constructor: from params
        public CharacterWrapper(
            string name,
            string id,
            int level,
            CultureObject culture,
            List<(SkillObject skill, int value)> skills,
            List<Equipment> equipments,
            CharacterObject[] upgradeTargets = null,
            ItemCategory upgradeRequiresItemFromCategory = null,
            Occupation occupation = DefaultOccupation)
        {
            CreateCharacterObject(name, id, level, occupation, culture, skills, equipments, upgradeTargets, upgradeRequiresItemFromCategory);
        }

        private void CreateCharacterObject(
            string name,
            string id,
            int level,
            Occupation occupation,
            CultureObject culture,
            List<(SkillObject skill, int value)> skills,
            List<Equipment> equipments,
            CharacterObject[] upgradeTargets,
            ItemCategory upgradeRequiresItemFromCategory)
        {

            // Ensure id is prefixed with IdPrefix
            string finalId = id.StartsWith(IdPrefix) ? id : IdPrefix + id;
            _characterObject = MBObjectManager.Instance.CreateObject<CharacterObject>(finalId);

            Log.Debug($"CharacterWrapper: Created CharacterObject for id={finalId}");

            // Set UpgradeTargets
            try
            {
                SetUpgradeTargets(upgradeTargets ?? new CharacterObject[0]);
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception setting UpgradeTargets: {ex}");
            }

            // Set UpgradeRequiresItemFromCategory
            try
            {
                SetUpgradeRequiresItemFromCategory(upgradeRequiresItemFromCategory);
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception setting UpgradeRequiresItemFromCategory: {ex}");
            }

            try
            {
                PropertyInfo bodyPropertyRange = Reflector.P<CharacterObject>(_characterObject, "BodyPropertyRange");
                bodyPropertyRange.SetValue(_characterObject, MBObjectManager.Instance.GetObject<MBBodyProperty>("fighter_empire"));
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception setting BodyPropertyRange: {ex}");
            }

            try
            {
                _characterObject.StringId = finalId;
                _characterObject.Culture = culture;
                _characterObject.Level = level;
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception setting StringId/Culture/Level: {ex}");
            }

            try
            {
                SetName(name);
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception setting Name: {ex}");
            }

            try
            {
                SetOccupation(occupation);
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception setting Occupation: {ex}");
            }

            try
            {
                SetSkills(skills);
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception setting Skills: {ex}");
            }

            try
            {
                SetEquipments(equipments);
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception setting Equipments: {ex}");
            }

            try
            {
                UpdateBodyProperties();
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception updating BodyProperties: {ex}");
            }

            try
            {
                _characterObject.HiddenInEncylopedia = false;
            }
            catch (System.Exception ex)
            {
                Log.Error($"CharacterWrapper: Exception updating HiddenInEncyclopedia: {ex}");
            }

        }

        private void UpdateBodyProperties()
        {
            ((BasicCharacterObject)_characterObject).UpdatePlayerCharacterBodyProperties(((BasicCharacterObject)_characterObject).BodyPropertyRange.BodyPropertyMax, ((BasicCharacterObject)_characterObject).Race, ((BasicCharacterObject)_characterObject).IsFemale);
            ((BasicCharacterObject)_characterObject).UpdatePlayerCharacterBodyProperties(((BasicCharacterObject)_characterObject).BodyPropertyRange.BodyPropertyMin, ((BasicCharacterObject)_characterObject).Race, ((BasicCharacterObject)_characterObject).IsFemale);
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

        private void SetOccupation(Occupation value)
        {
            // Try CharacterObject.Occupation setter
            try
            {
                var p = Reflector.P(typeof(CharacterObject), "Occupation");
                var set = p?.GetSetMethod(true);
                if (set != null)
                {
                    set.Invoke(_characterObject, new object[] { value });
                    Log.Debug($"Set Occupation={value} on {((MBObjectBase)_characterObject).StringId} (via CharacterObject property)");
                    return;
                }
            }
            catch { }

            // Try BasicCharacterObject.Occupation setter
            try
            {
                var p = Reflector.P(typeof(BasicCharacterObject), "Occupation");
                var set = p?.GetSetMethod(true);
                if (set != null)
                {
                    set.Invoke(_characterObject, new object[] { value });
                    Log.Info($"Set Occupation={value} on {((MBObjectBase)_characterObject).StringId} (via BasicCharacterObject property)");
                    return;
                }
            }
            catch { }

            // Try backing field on obj or base types
            try
            {
                var t = _characterObject.GetType();
                while (t != null)
                {
                    foreach (var fi in t.GetFields(Reflector.Flags))
                    {
                        if (fi.FieldType == typeof(Occupation))
                        {
                            fi.SetValue(_characterObject, value);
                            Log.Info($"Set Occupation={value} on {((MBObjectBase)_characterObject).StringId} (via field {t.Name}.{fi.Name})");
                            return;
                        }
                    }
                    t = t.BaseType;
                }
            }
            catch { }

            Log.Warn($"WARNING: failed to set Occupation on {((MBObjectBase)_characterObject).StringId}");
            return;
        }

        public Occupation GetOccupation()
        {
            PropertyInfo property = Reflector.P<BasicCharacterObject>(_characterObject, "Occupation");
            return (Occupation)property.GetValue(_characterObject);
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
            MBEquipmentRoster roster = new MBEquipmentRoster();
            MBList<Equipment> mbList = new MBList<Equipment>(equipments);
            FieldInfo field2 = Reflector.F<MBEquipmentRoster>(roster, "_equipments");
            field2.SetValue(roster, mbList);
            FieldInfo field3 = Reflector.F<BasicCharacterObject>((BasicCharacterObject)(object)_characterObject, "_equipmentRoster");
            field3.SetValue(_characterObject, roster);
            ((BasicCharacterObject)_characterObject).InitializeEquipmentsOnLoad((BasicCharacterObject)(object)_characterObject);
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

        private CharacterObject[] GetUpgradeTargets()
        {
            var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeTargets");
            var value = prop.GetValue(_characterObject) as CharacterObject[];
            return value ?? new CharacterObject[0];
        }

        private void SetUpgradeTargets(CharacterObject[] targets)
        {
            var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeTargets");
            prop.SetValue(_characterObject, targets ?? new CharacterObject[0]);
        }

        private ItemCategory GetUpgradeRequiresItemFromCategory()
        {
            var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeRequiresItemFromCategory");
            return (ItemCategory)prop.GetValue(_characterObject);
        }

        private void SetUpgradeRequiresItemFromCategory(ItemCategory value)
        {
            var prop = Reflector.P<CharacterObject>(_characterObject, "UpgradeRequiresItemFromCategory");
            prop.SetValue(_characterObject, value);
        }
    }
}
