using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Module;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
    {
        /// <summary>
        /// Skill IDs for combat skills.
        /// </summary>
        private static readonly HashSet<string> CombatSkillIds =
        [
            "OneHanded",
            "TwoHanded",
            "Polearm",
            "Bow",
            "Crossbow",
            "Throwing",
            "Riding",
            "Athletics",
        ];

        /// <summary>
        /// Skill IDs for non-combat "hero" skills.
        /// </summary>
        public static readonly HashSet<string> HeroSkillIds =
        [
            "Crafting",
            "Tactics",
            "Scouting",
            "Roguery",
            "Charm",
            "Leadership",
            "Trade",
            "Steward",
            "Medicine",
            "Engineering",
        ];

        /// <summary>
        /// Skill IDs added by the Naval DLC.
        /// </summary>
        public static readonly HashSet<string> NavalDLCSkillIds =
        [
            "Mariner",
            "Boatswain",
            "Shipmaster",
        ];

        /// <summary>
        /// All known skill IDs, including combat, hero, and naval DLC skills.
        /// </summary>
        private static readonly HashSet<string> AllSkillIds =
        [
            .. CombatSkillIds,
            .. HeroSkillIds,
            .. NavalDLCSkillIds,
        ];

        /// <summary>
        /// List of modded skills not part of the base game.
        /// </summary>
        private static List<SkillObject> _moddedSkills;
        public static List<SkillObject> ModdedSkills =>
            _moddedSkills ??= [
                .. MBObjectManager
                    .Instance.GetObjectTypeList<SkillObject>()
                    .Where(s => !AllSkillIds.Contains(s.StringId)),
            ];

        /// <summary>
        /// Gets the list of skills applicable to the given character.
        /// </summary>
        public static List<SkillObject> GetSkillList(
            WCharacter character,
            bool includeExtras = false
        )
        {
            var skills = new List<SkillObject>();

            // Always include combat skills.
            skills.AddRange(CombatSkillIds.Select(IdToSkill).Where(s => s != null));

            // Modded skills, if any, are extras.
            if (includeExtras)
                skills.AddRange(ModdedSkills.Where(s => s != null));

            if (character.IsHero)
            {
                // Include hero skills.
                skills.AddRange(HeroSkillIds.Select(IdToSkill).Where(s => s != null));

                // Include naval DLC skills if the DLC is loaded, as extras.
                if (includeExtras && Mods.NavalDLC.IsLoaded)
                    skills.AddRange(NavalDLCSkillIds.Select(IdToSkill).Where(s => s != null));
            }

            return skills;
        }

        /// <summary>
        /// Converts a skill ID to a SkillObject.
        /// </summary>
        private static SkillObject IdToSkill(string skillId) =>
            MBObjectManager.Instance.GetObject<SkillObject>(skillId);

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
                _skills ??= new CharacterSkills(this);
                return _skills;
            }
        }

        /* ━━━ Attribute Helper ━━━ */

        [SafeClass]
        public class CharacterSkills
        {
            readonly WCharacter _wc;

            /* ━━━━━━ Constructor ━━━━━ */

            public CharacterSkills(WCharacter wc)
            {
                _wc = wc;

                foreach (var skill in GetSkillList(_wc))
                {
                    if (skill == null)
                        continue;

                    _attributes[skill] = MakeSkillAttribute(skill);
                }
            }

            /* ━━━━━━ Attributes ━━━━━━ */

            private readonly Dictionary<SkillObject, MAttribute<int>> _attributes =
                new Dictionary<SkillObject, MAttribute<int>>();

            private MAttribute<int> MakeSkillAttribute(SkillObject skill) =>
                new(
                    baseInstance: _wc.Base, // anchor persistence on the CharacterObject
                    getter: _ => _wc.Base.GetSkillValue(skill),
                    setter: (_, value) => SetSkill(skill, value),
                    targetName: $"skill_{skill.StringId}", // stable per-skill key
                    persistent: true
                );

            private void SetSkill(SkillObject skill, int value)
            {
                var skills = Reflection.GetFieldValue<MBCharacterSkills>(
                    _wc.Base,
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
                return _wc.Base.GetSkillValue(skill);
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
                    // Lazily support skills not in known lists (e.g. modded ones)
                    attribute = MakeSkillAttribute(skill);
                    _attributes[skill] = attribute;
                    attribute.Set(value);
                }
            }

            public void Modify(SkillObject skill, int amount)
            {
                if (skill == null)
                    return;

                var current = Get(skill);
                Set(skill, current + amount);
            }
        }
    }
}
