using System;
using System.Collections;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Model.Attributes;
using Retinues.Framework.Runtime;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Services.Skills
{
    [SafeClass]
    public class Skills : IEnumerable<(SkillObject Skill, int Value)>
    {
        readonly WCharacter _wc;

        readonly List<SkillObject> _validSkills;

        readonly Dictionary<SkillObject, MAttribute<int>> _attributes = [];

        protected Skills() { }

        public Skills(WCharacter wc)
        {
            _wc = wc;

            _validSkills = SkillCatalog.GetSkills(_wc) ?? [];

            for (int i = 0; i < _validSkills.Count; i++)
            {
                var skill = _validSkills[i];
                if (skill == null)
                    continue;

                _attributes[skill] = _wc.MakeSkillAttribute(skill);
            }
        }

        public virtual int GetBase(SkillObject skill)
        {
            if (skill == null)
                return 0;

            if (_wc == null)
                return Get(skill);

            if (_attributes.TryGetValue(skill, out var attribute))
                return attribute.Get();

            return _wc.Base.GetSkillValue(skill);
        }

        public virtual int GetStaged(SkillObject skill)
        {
            if (_wc == null)
                return 0;

            if (skill == null || string.IsNullOrEmpty(skill.StringId))
                return 0;

            return _wc.GetStagedSkillDelta(skill.StringId);
        }

        public virtual bool IsStaged(SkillObject skill)
        {
            if (_wc == null)
                return false;

            return WCharacter.IsSkillStagingActive(_wc) && GetStaged(skill) > 0;
        }

        public virtual int Get(SkillObject skill)
        {
            var baseValue = GetBase(skill);

            if (_wc == null || !WCharacter.IsSkillStagingActive(_wc))
                return baseValue;

            return baseValue + GetStaged(skill);
        }

        public virtual void Set(SkillObject skill, int value)
        {
            if (_wc == null || skill == null)
                return;

            if (!_attributes.TryGetValue(skill, out var attribute))
            {
                // Still allow setting any SkillObject, but iteration remains restricted
                // to the catalog's valid list for this character.
                attribute = _wc.MakeSkillAttribute(skill);
                _attributes[skill] = attribute;
            }

            attribute.Set(value);

            // Invalidate conversion sources cache for retinues.
            _wc.ConversionCache.Clear();
        }

        public virtual void Modify(SkillObject skill, int amount)
        {
            if (skill == null || amount == 0)
                return;

            if (_wc == null)
            {
                Set(skill, Get(skill) + amount);
                return;
            }

            if (!WCharacter.IsSkillStagingActive(_wc))
            {
                var current = Get(skill);
                Set(skill, current + amount);
                return;
            }

            var id = skill.StringId;
            if (string.IsNullOrEmpty(id))
                return;

            if (amount > 0)
            {
                _wc.AddStagedSkillDelta(id, amount);
                return;
            }

            var remaining = -amount;

            var staged = _wc.GetStagedSkillDelta(id);
            if (staged > 0)
            {
                var consume = Math.Min(staged, remaining);
                _wc.AddStagedSkillDelta(id, -consume);
                remaining -= consume;
            }

            if (remaining > 0)
            {
                var currentBase = GetBase(skill);
                Set(skill, currentBase - remaining);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Indexer                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int this[SkillObject skill]
        {
            get => Get(skill);
            set => Set(skill, value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Enumeration                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Enumerates all valid skills and their effective values.
        /// </summary>
        public virtual IEnumerator<(SkillObject Skill, int Value)> GetEnumerator()
        {
            if (_validSkills == null)
                yield break;

            for (int i = 0; i < _validSkills.Count; i++)
            {
                var s = _validSkills[i];
                if (s == null)
                    continue;

                yield return (s, Get(s));
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Provides skill access and manipulation for a hero.
    /// </summary>
    [SafeClass]
    public sealed class HeroSkills : Skills
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
        //                        Get / Set                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the specified skill value for the hero.
        /// </summary>
        public override int Get(SkillObject skill)
        {
            if (skill == null)
                return 0;

            return _hero.Base.GetSkillValue(skill);
        }

        /// <summary>
        /// Sets the specified skill value for the hero.
        /// </summary>
        public override void Set(SkillObject skill, int value)
        {
            if (skill == null)
                return;

            _hero.Base.SetSkillValue(skill, value);
        }

        /// <summary>
        /// Adjusts the specified skill by the given amount.
        /// </summary>
        public override void Modify(SkillObject skill, int amount)
        {
            if (skill == null || amount == 0)
                return;

            Set(skill, Get(skill) + amount);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Enumeration                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Enumerates valid skills and their current values for this hero.
        /// </summary>
        public override IEnumerator<(SkillObject Skill, int Value)> GetEnumerator()
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
