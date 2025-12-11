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
    }
}
