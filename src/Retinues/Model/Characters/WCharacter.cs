using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Equipments;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
#if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Model.Characters
{
    public class WCharacter(CharacterObject @base) : WBase<WCharacter, CharacterObject>(@base)
    {
        public const string CustomTroopPrefix = "retinues_custom_";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Tier ━━━━━━━━━ */

        public int Tier => Base.Tier;

        /* ━━━━━━━━━ Level ━━━━━━━━ */

        MAttribute<int> LevelAttribute => Attribute<int>(nameof(CharacterObject.Level));

        public int Level
        {
            get => LevelAttribute.Get();
            set => LevelAttribute.Set(value);
        }

        /* ━━━━━━━━━ Name ━━━━━━━━━ */

        MAttribute<TextObject> NameAttribute => Attribute<TextObject>("_basicName");

        public string Name
        {
            get => NameAttribute.Get().ToString();
            set => NameAttribute.Set(new TextObject(value));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsHero => Base.IsHero;
        public bool IsPlayer => Base.IsPlayerCharacter;
        public bool IsCustom => StringId.StartsWith(CustomTroopPrefix);
        public bool IsVanilla => !IsCustom;
        public bool IsRoot => Root == this;
        public bool IsLeaf => UpgradeTargets.Count == 0;
        public bool IsElite => Root == Culture.RootElite;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<CultureObject> CultureAttribute => Attribute(c => c.Culture);

        public WCulture Culture
        {
            get => WCulture.Get(CultureAttribute.Get());
            set => CultureAttribute.Set(value?.Base);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Visuals                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ IsFemale ━━━━━━━ */

        MAttribute<bool> IsFemaleAttribute => Attribute<bool>(nameof(CharacterObject.IsFemale));

        public bool IsFemale
        {
            get => IsFemaleAttribute.Get();
            set => IsFemaleAttribute.Set(value);
        }

        MAttribute<int> RaceAttribute => Attribute<int>(nameof(CharacterObject.Race));

        /* ━━━━━━━━━ Race ━━━━━━━━━ */

        public int Race
        {
            get => RaceAttribute.Get();
            set => RaceAttribute.Set(value);
        }

        MAttribute<float> AgeAttribute => Attribute<float>(nameof(CharacterObject.Age));

        /* ━━━━━━━━━━ Age ━━━━━━━━━ */

        public float Age
        {
            get => AgeAttribute.Get();
            set => AgeAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public FormationClass FormationClass => Base.GetFormationClass();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equipment                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<MBEquipmentRoster> EquipmentRosterAttribute =>
            Attribute<MBEquipmentRoster>("_equipmentRoster");

        public WEquipmentRoster EquipmentRoster
        {
            get => WEquipmentRoster.Get(EquipmentRosterAttribute.Get());
            set => EquipmentRosterAttribute.Set(value?.Base);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Skill Points                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int SkillPoints
        {
            get => SkillPointsAttribute.Get();
            set => SkillPointsAttribute.Set(value);
        }

        int _skillPoints;

        MAttribute<int> _skillPointsAttribute;
        MAttribute<int> SkillPointsAttribute =>
            _skillPointsAttribute ??= new MAttribute<int>(
                baseInstance: Base,
                getter: _ => _skillPoints,
                setter: (_, value) => _skillPoints = value,
                targetName: "skill_points"
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━ Skills Container ━━━ */

        private CharacterSkills _skills;
        public CharacterSkills Skills
        {
            get
            {
                _skills ??= new CharacterSkills(Base);
                return _skills;
            }
        }

        /* ━━━ Attribute Helper ━━━ */

        [SafeClass]
        public class CharacterSkills
        {
            readonly CharacterObject _co;

            /* ━━━━━━━━━ List ━━━━━━━━━ */

            // Known skills in a fixed order (for UI etc.).
            private static readonly SkillObject[] KnownSkills =
            [
                // Combat skills
                DefaultSkills.Athletics,
                DefaultSkills.Riding,
                DefaultSkills.OneHanded,
                DefaultSkills.TwoHanded,
                DefaultSkills.Polearm,
                DefaultSkills.Bow,
                DefaultSkills.Crossbow,
                DefaultSkills.Throwing,
                // Hero skills
                DefaultSkills.Crafting,
                DefaultSkills.Scouting,
                DefaultSkills.Tactics,
                DefaultSkills.Roguery,
                DefaultSkills.Charm,
                DefaultSkills.Leadership,
                DefaultSkills.Trade,
                DefaultSkills.Steward,
                DefaultSkills.Medicine,
                DefaultSkills.Engineering,
            ];

            public IReadOnlyList<SkillObject> All => KnownSkills;

            /* ━━━━━━ Constructor ━━━━━ */

            public CharacterSkills(CharacterObject co)
            {
                _co = co;

                foreach (var skill in KnownSkills)
                {
                    if (skill == null)
                        continue;

                    _attributes[skill] = MakeSkillAttribute(skill);
                }
            }

            /* ━━━━━━ Attributes ━━━━━━ */

            private readonly Dictionary<SkillObject, MAttribute<int>> _attributes = [];

            private MAttribute<int> MakeSkillAttribute(SkillObject skill) =>
                new(
                    baseInstance: _co, // anchor persistence on the CharacterObject
                    getter: _ => _co.GetSkillValue(skill),
                    setter: (_, value) => SetSkill(skill, value),
                    targetName: $"skill_{skill.StringId}", // stable per-skill key
                    persistent: true
                );

            private void SetSkill(SkillObject skill, int value)
            {
                var skills = Reflection.GetFieldValue<MBCharacterSkills>(
                    _co,
                    "DefaultCharacterSkills"
                );
                ((PropertyOwner<SkillObject>)(object)skills.Skills).SetPropertyValue(skill, value);
            }

            /* ━━━━━━━ Get / Set ━━━━━━ */

            public int Get(SkillObject skill)
            {
                if (skill == null)
                    return 0;

                if (_attributes.TryGetValue(skill, out var attribute))
                    return attribute.Get();

                // Fallback for unexpected/mod-added skills
                return _co.GetSkillValue(skill);
            }

            public void Set(SkillObject skill, int value)
            {
                if (skill == null)
                    return;

                if (_attributes.TryGetValue(skill, out var attribute))
                {
                    attribute.Set(value);
                }
                else
                {
                    // Lazily support skills not in KnownSkills (e.g. modded ones)
                    attribute = MakeSkillAttribute(skill);
                    _attributes[skill] = attribute;
                    attribute.Set(value);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Code ━━━━━━━━━ */

        public CharacterCode GetCharacterCode(bool civilian = false) =>
            CharacterCode.CreateFrom(
# if BL13
                Base,
                civilian ? Base.FirstCivilianEquipment : Base.FirstBattleEquipment
# else
                Base // No equipment type parameter in BL12
# endif
            );

        /* ━━━━━━━━━ Image ━━━━━━━━ */

# if BL13
        public CharacterImageIdentifierVM GetImage(bool civilian = false) =>
#else
        public ImageIdentifierVM GetImage(bool civilian = false) =>
# endif
            new(GetCharacterCode(civilian));

        /* ━━━ Image Identifier ━━━ */

        public ImageIdentifier GetImageIdentifier(bool civilian = false) =>
# if BL13
            new CharacterImageIdentifier(GetCharacterCode(civilian));
#else
            new(GetCharacterCode(civilian));
# endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Character Tree                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━ Upgrade Targets ━━━ */

        MAttribute<List<WCharacter>> _upgradeTargetsAttribute;
        MAttribute<List<WCharacter>> UpgradeTargetsAttribute =>
            _upgradeTargetsAttribute ??= new MAttribute<List<WCharacter>>(
                baseInstance: Base,
                getter: _ => [.. Base.UpgradeTargets.Select(Get)],
                setter: (_, list) =>
                {
                    // Update the underlying CharacterObject's UpgradeTargets.
                    Reflection.SetPropertyValue(
                        Base,
                        "UpgradeTargets",
                        list.Select(w => w?.Base).ToList()
                    );

                    // Keep hierarchy cache in sync whenever targets change.
                    CharacterTreeCacheHelper.RecomputeForRoot(Root);
                },
                targetName: "UpgradeTargets"
            );

        public List<WCharacter> UpgradeTargets
        {
            get => UpgradeTargetsAttribute.Get();
            set => UpgradeTargetsAttribute.Set(value ?? []);
        }

        /* ━━━ Cached Properties ━━ */

        public List<WCharacter> UpgradeSources => CharacterTreeCacheHelper.GetUpgradeSources(this);
        public int Depth => CharacterTreeCacheHelper.GetDepth(this);
        public WCharacter Root => CharacterTreeCacheHelper.GetRoot(this) ?? this;
        public List<WCharacter> Tree => CharacterTreeCacheHelper.GetTree(this);
    }
}
