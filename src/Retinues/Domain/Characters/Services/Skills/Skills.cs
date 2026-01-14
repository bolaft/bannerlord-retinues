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
    public sealed class Skills : ICharacterSkills, IEnumerable<(SkillObject Skill, int Value)>
    {
        readonly WCharacter _wc;

        readonly List<SkillObject> _validSkills;

        readonly Dictionary<SkillObject, MAttribute<int>> _attributes = [];

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Indexer                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int this[SkillObject skill]
        {
            get => Get(skill);
            set => Set(skill, value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int GetBase(SkillObject skill)
        {
            if (skill == null)
                return 0;

            if (_attributes.TryGetValue(skill, out var attribute))
                return attribute.Get();

            return _wc.Base.GetSkillValue(skill);
        }

        public int GetStaged(SkillObject skill)
        {
            if (skill == null || string.IsNullOrEmpty(skill.StringId))
                return 0;

            return _wc.GetStagedSkillDelta(skill.StringId);
        }

        public bool IsStaged(SkillObject skill) =>
            WCharacter.IsSkillStagingActive(_wc) && GetStaged(skill) > 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Get / Set                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int Get(SkillObject skill)
        {
            var baseValue = GetBase(skill);

            if (!WCharacter.IsSkillStagingActive(_wc))
                return baseValue;

            return baseValue + GetStaged(skill);
        }

        public void Set(SkillObject skill, int value)
        {
            if (skill == null)
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

        public void Modify(SkillObject skill, int amount)
        {
            if (skill == null || amount == 0)
                return;

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
        //                       Enumeration                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
