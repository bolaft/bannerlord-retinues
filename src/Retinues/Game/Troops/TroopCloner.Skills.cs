using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Game.Troops
{
    public static partial class TroopCloner
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Skills                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly struct SkillValue(SkillObject skill, int value, float scaled, int floor)
        {
            public readonly SkillObject Skill = skill;
            public readonly int Value = value;
            public readonly float Scaled = scaled;
            public readonly int Floor = floor;
        }

        private static void EnforceSkillLimits(WCharacter wc)
        {
            if (wc == null || wc.Base == null)
                return;

            if (wc.IsHero)
                return;

            var cap = wc.SkillCapForTier;
            var totalMax = wc.SkillTotalMaxForTier;

            if (cap <= 0 || totalMax <= 0)
                return;

            var skills = SkillsHelper.GetSkillListForCharacter(
                isHeroLike: false,
                includeModded: true
            );

            if (skills == null || skills.Count == 0)
                return;

            var values = new List<(SkillObject skill, int value)>(skills.Count);
            int total = 0;

            for (int i = 0; i < skills.Count; i++)
            {
                var s = skills[i];
                if (s == null)
                    continue;

                int v = wc.Base.GetSkillValue(s);

                if (v < 0)
                    v = 0;

                if (v > cap)
                    v = cap;

                values.Add((s, v));
                total += v;
            }

            if (values.Count == 0)
                return;

            if (total <= totalMax)
            {
                for (int i = 0; i < values.Count; i++)
                    wc.Skills.Set(values[i].skill, values[i].value);

                return;
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

            for (int i = 0; i < skills.Count; i++)
            {
                var s = skills[i];
                if (s == null || string.IsNullOrEmpty(s.StringId))
                    continue;

                if (final.TryGetValue(s.StringId, out var v))
                    wc.Skills.Set(s, v);
            }
        }
    }
}
