using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Editor;
using Retinues.Framework.Model.Attributes;
using Retinues.Settings;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Characters.Wrappers
{
    /// <summary>
    /// Skill staging (deferred training) for wrapped character templates and clones.
    /// </summary>
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Skills Staging                     //
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

            if (!Configuration.TrainingTakesTime)
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
    }
}
