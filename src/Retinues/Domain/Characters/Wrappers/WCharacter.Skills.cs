using System;
using System.Linq;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Framework.Model.Attributes;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Wrappers
{
    /// <summary>
    /// Skill and skill-point management for wrapped character templates and clones.
    /// </summary>
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Skill Points                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<int> SkillPointsAttribute => Attribute(initialValue: 0);

        public int SkillPoints
        {
            get => NonVariantBase().SkillPointsAttribute.Get();
            set => NonVariantBase().SkillPointsAttribute.Set(value);
        }

        MAttribute<int> SkillPointsExperienceAttribute => Attribute(initialValue: 0);

        /// <summary>
        /// Experience accumulated toward the next skill point for this unit.
        /// </summary>
        public int SkillPointsExperience
        {
            get => NonVariantBase().SkillPointsExperienceAttribute.Get();
            set => NonVariantBase().SkillPointsExperienceAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━ Skills Container ━━━ */

        private Skills _skills;

        /// <summary>
        /// Container exposing runtime skill values for this character.
        /// </summary>
        public virtual Skills Skills
        {
            get
            {
                if (IsHero)
                    return Hero.Skills;

                _skills ??= new Skills(this);
                return _skills;
            }
        }

        /// <summary>
        /// Clears the cached Skills container so it will be rebuilt.
        /// </summary>
        public void ClearSkillsCache()
        {
            if (IsHero)
                Hero.ClearSkillsCache();

            _skills = null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Skill Rules                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Maximum per-skill cap for this character.
        /// </summary>
        public int SkillCap => SkillRules.GetSkillCap(this);

        /// <summary>
        /// Total skill points available for this character.
        /// </summary>
        public int SkillTotal => SkillRules.GetSkillTotal(this);

        /// <summary>
        /// Sum of currently assigned skill levels.
        /// </summary>
        public int SkillTotalUsed => Skills.Sum(skill => skill.Value);

        /// <summary>
        /// Remaining unspent skill points.
        /// </summary>
        public int SkillTotalRemaining => Math.Max(0, SkillTotal - SkillTotalUsed);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Attributes                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates a persistent per-skill attribute wired to the underlying CharacterObject skills.
        /// Skills use Lowest priority so they are always applied last during deserialization,
        /// after Level and other attributes that may cause CharacterObject skill recalculation.
        /// </summary>
        public MAttribute<int> MakeSkillAttribute(SkillObject skill) =>
            Attribute(
                getter: _ => Base.GetSkillValue(skill),
                setter: (_, value) => SetBaseSkillValue(skill, value),
                priority: AttributePriority.Lowest,
                name: $"Skill_{skill.StringId}"
            );

        private void SetBaseSkillValue(SkillObject skill, int value)
        {
            var skills = Reflection.GetFieldValue<MBCharacterSkills>(
                Base,
                "DefaultCharacterSkills"
            );

            ((PropertyOwner<SkillObject>)(object)skills.Skills).SetPropertyValue(skill, value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Skills Persistence                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<int> SkillsBootstrapAttribute
        {
            get
            {
                _ = Skills;

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
