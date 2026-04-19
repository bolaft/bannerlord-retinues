using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Compatibility;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Modules.Versions;
using Retinues.Settings;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Troops
{
    /// <summary>
    /// Partial Cloner utilities for handling skill limits and normalization on cloned troops.
    /// </summary>
    public static partial class Cloner
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Helper tuple that holds original and scaled skill values for redistribution.
        /// </summary>
        private readonly struct SkillValue(SkillObject skill, int value, float scaled, int floor)
        {
            public readonly SkillObject Skill = skill;
            public readonly int Value = value;
            public readonly float Scaled = scaled;
            public readonly int Floor = floor;
        }

        /// <summary>
        /// Collects the StringIds of skills that are relevant to the given template's battle
        /// equipment (i.e. skills used by any equipped weapon or mount, plus Mariner if applicable).
        /// </summary>
        private static HashSet<string> CollectRelevantSkillIds(WCharacter template)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);

            if (template?.Base == null)
                return ids;

            var slots = new[]
            {
                EquipmentIndex.WeaponItemBeginSlot,
                EquipmentIndex.Weapon1,
                EquipmentIndex.Weapon2,
                EquipmentIndex.Weapon3,
                EquipmentIndex.Horse,
            };

            foreach (var eq in template.Equipments)
            {
                if (eq == null || eq.IsCivilian)
                    continue;

                foreach (var slot in slots)
                {
                    var item = eq.Get(slot);
                    if (item?.Base == null)
                        continue;

                    // Horse slot: always consider Riding relevant.
                    if (slot == EquipmentIndex.Horse)
                    {
                        ids.Add("Riding");
                        continue;
                    }

                    if (item.RelevantSkill != null)
                        ids.Add(item.RelevantSkill.StringId);
                }
            }

            // Mariner: relevant only if troop has the trait, DLC is loaded, and BL 1.4+.
            if (template.IsMariner && Mods.NavalDLC.IsLoaded && GameVersion.IsAtLeast14())
                ids.Add(SkillCatalog.MarinerSkillId);

            return ids;
        }

        /// <summary>
        /// Collects the minimum skill level required per skill to use all items in the template's
        /// battle equipment sets. Returns the max Difficulty found per RelevantSkill.
        /// </summary>
        private static Dictionary<string, int> CollectMinRequiredSkills(WCharacter template)
        {
            var reqs = new Dictionary<string, int>(StringComparer.Ordinal);

            if (template?.Base == null)
                return reqs;

            var slots = new[]
            {
                EquipmentIndex.WeaponItemBeginSlot,
                EquipmentIndex.Weapon1,
                EquipmentIndex.Weapon2,
                EquipmentIndex.Weapon3,
                EquipmentIndex.Horse,
                EquipmentIndex.HorseHarness,
            };

            foreach (var eq in template.Equipments)
            {
                if (eq == null || eq.IsCivilian)
                    continue;

                foreach (var slot in slots)
                {
                    var item = eq.Get(slot);
                    if (item?.Base == null || item.RelevantSkill == null || item.Difficulty <= 0)
                        continue;

                    var id = item.RelevantSkill.StringId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (!reqs.TryGetValue(id, out var existing) || item.Difficulty > existing)
                        reqs[id] = item.Difficulty;
                }
            }

            return reqs;
        }

        /// <summary>
        /// Applies the StarterSkills configuration mode to pre-process skill values on the clone
        /// before the standard cap enforcement runs.
        /// Only applied when <paramref name="template"/> is non-null.
        /// </summary>
        private static void PreprocessStarterSkills(WCharacter wc, WCharacter template)
        {
            var mode = Configuration.StarterSkills.Value;

            if (mode == Configuration.SkillMode.High)
                return; // High = keep all skills as copied; EnforceSkillLimits handles the rest.

            if (mode == Configuration.SkillMode.Low)
            {
                var relevant = CollectRelevantSkillIds(template);

                foreach (var (skill, value) in wc.Skills)
                {
                    if (string.IsNullOrEmpty(skill?.StringId) || !relevant.Contains(skill.StringId))
                    {
                        wc.Skills.Set(skill, 0);
                    }
                    else
                    {
                        // Scale by 0.66, round to nearest 10.
                        int scaled = (int)(Math.Round(value * 0.66 / 10.0) * 10);
                        if (scaled < 0)
                            scaled = 0;
                        wc.Skills.Set(skill, scaled);
                    }
                }
            }
            else if (mode == Configuration.SkillMode.Minimal)
            {
                var minReqs = CollectMinRequiredSkills(template);

                foreach (var (skill, _) in wc.Skills)
                {
                    if (skill?.StringId == null)
                        continue;

                    int req = minReqs.TryGetValue(skill.StringId, out var r) ? r : 0;
                    wc.Skills.Set(skill, req);
                }
            }
        }

        /// <summary>
        /// Enforces per-skill caps and total skill point limits on a cloned character.
        /// When <paramref name="template"/> is provided and <paramref name="fillToTotal"/> is false,
        /// the StarterSkills configuration mode is applied before capping.
        /// </summary>
        private static void EnforceSkillLimits(
            WCharacter wc,
            bool fillToTotal = false,
            WCharacter template = null
        )
        {
            if (wc == null || wc.Base == null)
                return;

            if (wc.IsHero)
                return;

            // Apply StarterSkills mode preprocessing before capping.
            if (!fillToTotal && template != null)
                PreprocessStarterSkills(wc, template);

            var cap = wc.SkillCap;
            var totalMax = wc.SkillTotal;

            if (cap <= 0 || totalMax <= 0)
                return;

            var values = new List<(SkillObject skill, int value)>();
            int total = 0;

            foreach (var (s, v) in wc.Skills)
            {
                if (v < 0)
                    wc.Skills.Set(s, 0);

                if (v > cap)
                    wc.Skills.Set(s, cap);

                values.Add((s, v));
                total += v;
            }

            if (values.Count == 0)
                return;

            if (total <= totalMax)
            {
                if (!fillToTotal || total == 0)
                {
                    for (int i = 0; i < values.Count; i++)
                        wc.Skills.Set(values[i].skill, values[i].value);

                    return;
                }
                // Fall through to scale up proportionally to fill the tier budget.
            }

            float factor = totalMax / (float)total;

            var scaled = new List<SkillValue>(values.Count);
            int sumFloor = 0;

            for (int i = 0; i < values.Count; i++)
            {
                var (skill, v) = values[i];

                float sv = v * factor;
                int fv = (int)Math.Floor(sv);

                if (fv < 0)
                    fv = 0;

                if (fv > cap)
                    fv = cap;

                scaled.Add(new SkillValue(skill, v, sv, fv));
                sumFloor += fv;
            }

            int remaining = totalMax - sumFloor;
            if (remaining < 0)
                remaining = 0;

            scaled.Sort(
                (a, b) =>
                {
                    float fa = a.Scaled - a.Floor;
                    float fb = b.Scaled - b.Floor;
                    return fb.CompareTo(fa);
                }
            );

            var final = new Dictionary<string, int>(scaled.Count, StringComparer.Ordinal);
            for (int i = 0; i < scaled.Count; i++)
                final[scaled[i].Skill.StringId] = scaled[i].Floor;

            for (int i = 0; i < scaled.Count && remaining > 0; i++)
            {
                var sv = scaled[i];
                var id = sv.Skill?.StringId;
                if (string.IsNullOrEmpty(id))
                    continue;

                int current = final[id];
                if (current >= cap)
                    continue;

                final[id] = current + 1;
                remaining--;
            }

            foreach (var s in values.Select(v => v.skill))
            {
                if (final.TryGetValue(s.StringId, out var v))
                    wc.Skills.Set(s, v);
            }
        }
    }
}
