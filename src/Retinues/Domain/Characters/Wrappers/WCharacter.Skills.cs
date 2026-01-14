using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Helpers;
using Retinues.Editor;
using Retinues.Framework.Model.Attributes;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

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

        MAttribute<int> SkillPointsExperienceAttribute => Attribute(initialValue: 0);

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

        private CharacterSkills _skills;
        public CharacterSkills Skills
        {
            get
            {
                _skills ??= new CharacterSkills(this);
                return _skills;
            }
        }

        public void ClearSkillsCache() => _skills = null;

        /* ━━━ Attribute Helper ━━━ */

        [SafeClass]
        public class CharacterSkills : ICharacterSkills
        {
            readonly WCharacter _wc;

            /* ━━━━━━ Constructor ━━━━━ */

            public CharacterSkills(WCharacter wc)
            {
                _wc = wc;

                foreach (var skill in SkillsHelper.GetSkillList(_wc))
                {
                    if (skill == null)
                        continue;

                    _attributes[skill] = MakeSkillAttribute(skill);
                }
            }

            /* ━━━━━━ Attributes ━━━━━━ */

            private readonly Dictionary<SkillObject, MAttribute<int>> _attributes = [];

            private MAttribute<int> MakeSkillAttribute(SkillObject skill) =>
                // Use the wrapper's Attribute helper to create a per-skill attribute. The
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

            /* ━━━━━━━ Helpers ━━━━━━━ */

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
                IsSkillStagingActive(_wc) && GetStaged(skill) > 0;

            /* ━━━━━━━ Get / Set ━━━━━━ */

            public int Get(SkillObject skill)
            {
                var baseValue = GetBase(skill);

                if (!IsSkillStagingActive(_wc))
                    return baseValue;

                return baseValue + GetStaged(skill);
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

                // Invalidate conversion sources cache for retinues.
                _wc.ConversionCache.Clear();
            }

            public void Modify(SkillObject skill, int amount)
            {
                if (skill == null || amount == 0)
                    return;

                // Default behavior (universal mode, setting off, heroes, outside editor):
                // apply instantly to the base skill.
                if (!IsSkillStagingActive(_wc))
                {
                    var current = Get(skill);
                    Set(skill, current + amount);
                    return;
                }

                // Staging behavior (player mode only, WCharacter only):
                // - increases are staged
                // - decreases consume staging first, then apply instantly to base
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
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Skill Rules (Tier)                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        const int MaxSkillLevel = 360;

        public int SkillCapForTier => !IsHero ? SkillsHelper.GetSkillCap(this) : MaxSkillLevel;

        public int SkillTotalMaxForTier =>
            !IsHero ? SkillsHelper.GetSkillTotal(this) : int.MaxValue;

        public int SkillTotalUsed
        {
            get
            {
                // Sum the currently relevant skills for this character type.
                // If staging is enabled, Skills.Get() returns base + staged.
                var list = SkillsHelper.GetSkillList(this);
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
        //                      Skills Staging                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<Dictionary<string, int>> SkillsStagingAttribute =>
            Attribute(initialValue: new Dictionary<string, int>(), name: "SkillsStaging");

        /// <summary>
        /// Map of staged skill increases by SkillObject.StringId.
        /// Stored as an attribute so it persists via the attribute store.
        /// </summary>
        public Dictionary<string, int> SkillsStaging
        {
            get => new(SkillsStagingAttribute.Get() ?? []);
            set => SkillsStagingAttribute.Set(value == null ? new() : new(value));
        }

        internal int GetStagedSkillDelta(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return 0;

            var map = SkillsStagingAttribute.Get();
            if (map == null)
                return 0;

            return map.TryGetValue(skillId, out var v) ? Math.Max(0, v) : 0;
        }

        internal bool HasStagedSkillDelta(string skillId) => GetStagedSkillDelta(skillId) > 0;

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

        public float SkillStagingProgress
        {
            get => SkillStagingProgressAttribute.Get();
            set => SkillStagingProgressAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Staged Training Helpers                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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
            int next = Math.Min(360, baseValue + 1);
            SetBaseSkillValue(skill, next);

            // Consume one staged point.
            AddStagedSkillDelta(pick, -1);

            newValue = next;
            return true;
        }

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cheats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("add_skill_points", "retinues")]
        public static string AddSkillPointsCommand(List<string> args)
        {
            if (args.Count < 2)
                return "Usage: add_skill_points <troop_id> <amount>";

            var troopId = args[0];
            var retinueName = string.Join(" ", args.GetRange(1, args.Count - 1));

            var troop = Get(troopId);
            if (troop == null)
                return $"Error: Troop with id '{troopId}' not found.";

            if (!int.TryParse(retinueName, out var amount))
                return $"Error: Invalid amount '{retinueName}'.";

            troop.SkillPoints += amount;

            return $"Added {amount} skill points to troop '{troop.Name}' ({troopId}).";
        }
    }
}
