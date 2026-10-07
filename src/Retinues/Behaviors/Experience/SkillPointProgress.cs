using System;

namespace Retinues.Behaviors.Experience
{
    internal static class SkillPointProgress
    {
        /// <summary>Convert each contribution at its own cost before sharing its progress.</summary>
        internal static int Add(ref double progress, int experience, int requiredExperience)
        {
            if (experience <= 0 || requiredExperience <= 0)
                return 0;

            progress += (double)experience / requiredExperience;
            // Avoid losing a point to rounding when several fractions sum to an integer.
            int points = (int)Math.Floor(progress + 1e-10);
            progress = Math.Max(0d, progress - points);
            return points;
        }

        internal static int MigrateLegacy(ref double progress, ref int legacyExperience, int cost)
        {
            if (cost <= 0)
                return 0;
            int points = Add(ref progress, legacyExperience, cost);
            legacyExperience = 0;
            return points;
        }
    }
}
