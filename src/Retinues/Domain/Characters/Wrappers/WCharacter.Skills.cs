using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Framework.Model.Attributes;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Skill Points                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<int> SkillPointsAttribute => Attribute(initialValue: 0);

        public int SkillPoints
        {
            get => SkillPointsAttribute.Get();
            set => SkillPointsAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Skill Rules (Tier)                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        const int MaxSkillLevel = 360;

        public int SkillCapForTier =>
            !IsHero ? Helpers.SkillsHelper.GetSkillCapForTier(Tier) : MaxSkillLevel;

        public int SkillTotalMaxForTier =>
            !IsHero ? Helpers.SkillsHelper.GetSkillTotalForTier(Tier) : int.MaxValue;

        public int SkillTotalUsed
        {
            get
            {
                // Sum the currently relevant skills for this character type.
                var list = Helpers.SkillsHelper.GetSkillListForCharacter(
                    IsHero,
                    includeModded: true
                );
                if (list == null || list.Count == 0)
                    return 0;

                int sum = 0;
                foreach (var s in list)
                    sum += Skills.Get(s);

                return sum;
            }
        }

        public int SkillTotalRemaining =>
            !IsHero ? Math.Max(0, SkillTotalMaxForTier - SkillTotalUsed) : int.MaxValue;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Interfacing ━━━━━ */

        ICharacterSkills ICharacter.Skills => Skills;

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
        public class CharacterSkills : ICharacterSkills
        {
            readonly WCharacter _wc;

            /* ━━━━━━ Constructor ━━━━━ */

            public CharacterSkills(WCharacter wc)
            {
                _wc = wc;

                foreach (
                    var skill in Helpers.SkillsHelper.GetSkillListForCharacter(
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

            private readonly Dictionary<SkillObject, MAttribute<int>> _attributes = [];

            private MAttribute<int> MakeSkillAttribute(SkillObject skill) =>
                // Use the wrapper's Attribute helper to create a per‑skill attribute. The
                // target name is stable across saves to persist each skill independently.
                _wc.Attribute(
                    getter: _ => _wc.Base.GetSkillValue(skill),
                    setter: (_, value) => SetSkill(skill, value),
                    name: $"Skill_{skill.StringId}"
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Skills Persistence                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<int> SkillsBootstrapAttribute
        {
            get
            {
                // Force creation of per-skill attributes (Skill_*)
                _ = Skills;

                // Dummy attribute so MBase.EnsureAttributesCreated sees an IMAttribute property.
                // Non-persistent so it never matters for saving.
                return Attribute(
                    getter: _ => 0,
                    setter: (_, __) => { },
                    persistent: false,
                    priority: AttributePriority.Low,
                    name: "__SkillsBootstrap"
                );
            }
        }
    }
}
