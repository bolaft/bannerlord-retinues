using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Editor;
using Retinues.Framework.Model.Attributes;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

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

        /// <summary>
        /// Skill points for this unit; captains may share the pool with their base troop.
        /// </summary>
        public int SkillPoints
        {
            get => ResolveSkillPointsAttribute().Get();
            set => ResolveSkillPointsAttribute().Set(value);
        }

        private MAttribute<int> ResolveSkillPointsAttribute()
        {
            if (!IsCaptain)
                return SkillPointsAttribute;

            var ownerId = SkillPointsOwnerIdAttribute.Get();
            if (
                string.IsNullOrEmpty(ownerId)
                || string.Equals(ownerId, StringId, StringComparison.Ordinal)
            )
                return SkillPointsAttribute;

            var owner = Get(ownerId);
            if (owner == null)
                return SkillPointsAttribute;

            // Avoid chaining captains to captains.
            if (owner.IsCaptain)
                return SkillPointsAttribute;

            return owner.SkillPointsAttribute;
        }

        MAttribute<int> SkillPointsExperienceAttribute => Attribute(initialValue: 0);

        /// <summary>
        /// Experience accumulated toward the next skill point for this unit.
        /// </summary>
        public int SkillPointsExperience
        {
            get => SkillPointsExperienceAttribute.Get();
            set => SkillPointsExperienceAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━ Interfacing ━━━━━ */

        ICharacterSkills ICharacterData.Skills => Skills;

        /* ━━━ Skills Container ━━━ */

        private Skills _skills;

        /// <summary>
        /// Container exposing runtime skill values for this character.
        /// </summary>
        public Skills Skills
        {
            get
            {
                _skills ??= new Skills(this);
                return _skills;
            }
        }

        /// <summary>
        /// Clears the cached Skills container so it will be rebuilt.
        /// </summary>
        public void ClearSkillsCache() => _skills = null;

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
        //                      Skills Staging                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<Dictionary<string, int>> SkillsStagingAttribute =>
            Attribute(initialValue: new Dictionary<string, int>(), name: "SkillsStaging");

        /// <summary>
        /// Map of staged skill increases persisted as attributes.
        /// </summary>
        public Dictionary<string, int> SkillsStaging
        {
            get => new(SkillsStagingAttribute.Get() ?? []);
            set => SkillsStagingAttribute.Set(value == null ? new() : new(value));
        }

        /// <summary>
        /// Returns the staged delta for the given skill id.
        /// </summary>
        internal int GetStagedSkillDelta(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return 0;

            var map = SkillsStagingAttribute.Get();
            if (map == null)
                return 0;

            return map.TryGetValue(skillId, out var v) ? Math.Max(0, v) : 0;
        }

        /// <summary>
        /// True if a positive staged delta exists for the given skill id.
        /// </summary>
        internal bool HasStagedSkillDelta(string skillId) => GetStagedSkillDelta(skillId) > 0;

        /// <summary>
        /// Adds or removes staged skill point deltas for the given skill id.
        /// </summary>
        internal void AddStagedSkillDelta(string skillId, int delta)
        {
            if (string.IsNullOrEmpty(skillId) || delta == 0)
                return;

            var current = SkillsStagingAttribute.Get() ?? [];
            var map = new Dictionary<string, int>(current);

            map.TryGetValue(skillId, out var oldValue);
            var next = Math.Max(0, oldValue + delta);

            if (next <= 0)
                map.Remove(skillId);
            else
                map[skillId] = next;

            if (map.Count == current.Count && next == oldValue)
                return;

            SkillsStagingAttribute.Set(map);
        }

        /// <summary>
        /// Whether staged skill training is active for the given non-hero character.
        /// </summary>
        internal static bool IsSkillStagingActive(WCharacter wc)
        {
            if (wc == null || wc.IsHero)
                return false;

            if (!Settings.TrainingTakesTime)
                return false;

            return EditorState.Instance.Mode == EditorMode.Player;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Skills Staging Progress                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<float> SkillStagingProgressAttribute =>
            Attribute(initialValue: 0f, name: "SkillStagingProgress");

        /// <summary>
        /// Progress toward applying staged skill points (fractional hours).
        /// </summary>
        public float SkillStagingProgress
        {
            get => SkillStagingProgressAttribute.Get();
            set => SkillStagingProgressAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Staged Training Helpers                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// True if any staged skill points remain to be applied.
        /// </summary>
        public bool HasAnyStagedSkillPoints()
        {
            var map = SkillsStagingAttribute.Get();
            if (map == null || map.Count == 0)
                return false;

            foreach (var kv in map)
            {
                if (kv.Value > 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to apply one staged skill point (randomly selected) and returns new value.
        /// </summary>
        internal bool TryApplyOneStagedSkillPointRandom(out SkillObject skill, out int newValue)
        {
            skill = null;
            newValue = 0;

            var map0 = SkillsStagingAttribute.Get();
            if (map0 == null || map0.Count == 0)
                return false;

            var keys = new List<string>();
            foreach (var kv in map0)
            {
                if (!string.IsNullOrEmpty(kv.Key) && kv.Value > 0)
                    keys.Add(kv.Key);
            }

            if (keys.Count == 0)
                return false;

            var pick = keys[MBRandom.RandomInt(keys.Count)];

            var manager = MBObjectManager.Instance;
            if (manager == null)
                return false;

            skill = manager.GetObject<SkillObject>(pick);
            if (skill == null)
            {
                AddStagedSkillDelta(pick, -int.MaxValue);
                return false;
            }

            // Apply to base skill (not staged).
            int baseValue = Base.GetSkillValue(skill);
            int next = Math.Min(SkillRules.MaxSkillLevel, baseValue + 1);
            SetBaseSkillValue(skill, next);

            // Consume one staged point.
            AddStagedSkillDelta(pick, -1);

            newValue = next;
            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Attributes                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates a persistent per-skill attribute wired to the underlying CharacterObject skills.
        /// </summary>
        public MAttribute<int> MakeSkillAttribute(SkillObject skill) =>
            Attribute(
                getter: _ => Base.GetSkillValue(skill),
                setter: (_, value) => SetBaseSkillValue(skill, value),
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
