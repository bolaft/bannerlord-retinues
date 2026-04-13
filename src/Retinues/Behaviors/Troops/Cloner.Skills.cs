using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
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
        /// Enforces per-skill caps and total skill point limits on a cloned character.
        /// </summary>
        private static void EnforceSkillLimits(WCharacter wc, bool fillToTotal = false)
        {
            if (wc == null || wc.Base == null)
                return;

            if (wc.IsHero)
                return;

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
