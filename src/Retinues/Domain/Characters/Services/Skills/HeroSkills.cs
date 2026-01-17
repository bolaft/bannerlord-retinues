using System.Collections;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Runtime;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Services.Skills
{
    /// <summary>
    /// Provides skill access and manipulation for a hero as an ICharacterSkills implementation.
    /// </summary>
    [SafeClass]
    public sealed class HeroSkills : ICharacterSkills, IEnumerable<(SkillObject Skill, int Value)>
    {
        readonly WHero _hero;

        readonly List<SkillObject> _validSkills;

        /// <summary>
        /// Creates a HeroSkills wrapper for the given hero and caches valid skills.
        /// </summary>
        public HeroSkills(WHero hero)
        {
            _hero = hero;
            _validSkills = SkillCatalog.GetSkills(_hero) ?? [];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Indexer                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Indexer to get or set a hero's skill value.
        /// </summary>
        public int this[SkillObject skill]
        {
            get => Get(skill);
            set => Set(skill, value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Get / Set                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the specified skill value for the hero.
        /// </summary>
        public int Get(SkillObject skill)
        {
            if (skill == null)
                return 0;

            return _hero.Base.GetSkillValue(skill);
        }

        /// <summary>
        /// Sets the specified skill value for the hero.
        /// </summary>
        public void Set(SkillObject skill, int value)
        {
            if (skill == null)
                return;

            _hero.Base.SetSkillValue(skill, value);
        }

        /// <summary>
        /// Adjusts the specified skill by the given amount.
        /// </summary>
        public void Modify(SkillObject skill, int amount)
        {
            if (skill == null || amount == 0)
                return;

            Set(skill, Get(skill) + amount);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Enumeration                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Non-generic enumerator forwarding to the generic enumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Enumerates valid skills and their current values for this hero.
        /// </summary>
        public IEnumerator<(SkillObject Skill, int Value)> GetEnumerator()
        {
            for (int i = 0; i < _validSkills.Count; i++)
            {
                var s = _validSkills[i];
                if (s == null)
                    continue;

                yield return (s, Get(s));
            }
        }
    }
}
