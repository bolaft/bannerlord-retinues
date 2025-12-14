using System.Collections.Generic;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
    {
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

                foreach (
                    var skill in Helpers.Skills.GetSkillListForCharacter(
                        _wc.IsHero,
                        includeModded: true
                    )
                )
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
