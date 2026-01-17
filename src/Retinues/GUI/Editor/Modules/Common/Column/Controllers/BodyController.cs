using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Services;
using Retinues.Utilities;

namespace Retinues.GUI.Editor.Modules.Common.Column.Controllers
{
    /// <summary>
    /// Controller for body-related appearance presets and actions.
    /// </summary>
    public class BodyController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Presets                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static readonly List<(float min, float max)> AgePresets =
        [
            (22, 29),
            (30, 49),
            (50, 69),
            (70, 99),
        ];

        static readonly List<(float min, float max)> WeightPresets =
        [
            (0.01f, 0.15f),
            (0.16f, 0.30f),
            (0.31f, 0.55f),
            (0.56f, 0.70f),
            (0.71f, 0.85f),
            (0.86f, 0.99f),
        ];

        static readonly List<(float min, float max)> BuildPresets =
        [
            (0.01f, 0.15f),
            (0.16f, 0.30f),
            (0.31f, 0.55f),
            (0.56f, 0.70f),
            (0.71f, 0.85f),
            (0.86f, 0.99f),
        ];

        static readonly List<(float min, float max)> HeightPresets =
        [
            (0.01f, 0.15f),
            (0.16f, 0.30f),
            (0.31f, 0.55f),
            (0.56f, 0.70f),
            (0.71f, 0.85f),
            (0.86f, 0.99f),
        ];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Decrease Age                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Decreases the character's age preset and triggers an appearance update.
        /// </summary>
        public static ControllerAction<WCharacter> DecreaseAge { get; } =
            Action<WCharacter>("DecreaseAge")
                .AddCondition(
                    c => CanStepPreset(AgePresets, c.AgeMin, c.AgeMax, -1),
                    L.T("body_age_min_reason", "Age is already at minimum.")
                )
                .DefaultTooltip(L.T("body_age_decrease_tip", "Decrease age"))
                .ExecuteWith(c => StepAgePreset(c, -1))
                .Fire(UIEvent.Appearance);

        /// <summary>
        /// Steps the age preset for the character by the given step (-1 or +1).
        /// </summary>
        private static void StepAgePreset(WCharacter c, int step)
        {
            StepPreset(
                c,
                AgePresets,
                step,
                () => c.AgeMin,
                v => c.AgeMin = v,
                () => c.AgeMax,
                v => c.AgeMax = v
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Increase Age                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Increases the character's age preset and triggers an appearance update.
        /// </summary>
        public static ControllerAction<WCharacter> IncreaseAge { get; } =
            Action<WCharacter>("IncreaseAge")
                .AddCondition(
                    c => CanStepPreset(AgePresets, c.AgeMin, c.AgeMax, +1),
                    L.T("body_age_max_reason", "Age is already at maximum.")
                )
                .DefaultTooltip(L.T("body_age_increase_tip", "Increase age"))
                .ExecuteWith(c => StepAgePreset(c, +1))
                .Fire(UIEvent.Appearance);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Decrease Height                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Decreases the character's height preset and triggers an appearance update.
        /// </summary>
        public static ControllerAction<WCharacter> DecreaseHeight { get; } =
            Action<WCharacter>("DecreaseHeight")
                .AddCondition(
                    c => CanStepPreset(HeightPresets, c.HeightMin, c.HeightMax, -1),
                    L.T("body_height_min_reason", "Height is already at minimum.")
                )
                .DefaultTooltip(L.T("body_height_decrease_tip", "Decrease height"))
                .ExecuteWith(c => StepHeightPreset(c, -1))
                .Fire(UIEvent.Appearance);

        /// <summary>
        /// Steps the height preset for the character by the given step (-1 or +1).
        /// </summary>
        private static void StepHeightPreset(WCharacter c, int step)
        {
            StepPreset(
                c,
                HeightPresets,
                step,
                () => c.HeightMin,
                v => c.HeightMin = v,
                () => c.HeightMax,
                v => c.HeightMax = v
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Increase Height                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Increases the character's height preset and triggers an appearance update.
        /// </summary>
        public static ControllerAction<WCharacter> IncreaseHeight { get; } =
            Action<WCharacter>("IncreaseHeight")
                .AddCondition(
                    c => CanStepPreset(HeightPresets, c.HeightMin, c.HeightMax, +1),
                    L.T("body_height_max_reason", "Height is already at maximum.")
                )
                .DefaultTooltip(L.T("body_height_increase_tip", "Increase height"))
                .ExecuteWith(c => StepHeightPreset(c, +1))
                .Fire(UIEvent.Appearance);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Decrease Weight                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Decreases the character's weight preset and triggers an appearance update.
        /// </summary>
        public static ControllerAction<WCharacter> DecreaseWeight { get; } =
            Action<WCharacter>("DecreaseWeight")
                .AddCondition(
                    c => CanStepPreset(WeightPresets, c.WeightMin, c.WeightMax, -1),
                    L.T("body_weight_min_reason", "Weight is already at minimum.")
                )
                .DefaultTooltip(L.T("body_weight_decrease_tip", "Decrease weight"))
                .ExecuteWith(c => StepWeightPreset(c, -1))
                .Fire(UIEvent.Appearance);

        /// <summary>
        /// Steps the weight preset for the character by the given step (-1 or +1).
        /// </summary>
        private static void StepWeightPreset(WCharacter c, int step)
        {
            StepPreset(
                c,
                WeightPresets,
                step,
                () => c.WeightMin,
                v => c.WeightMin = v,
                () => c.WeightMax,
                v => c.WeightMax = v
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Increase Weight                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Increases the character's weight preset and triggers an appearance update.
        /// </summary>
        public static ControllerAction<WCharacter> IncreaseWeight { get; } =
            Action<WCharacter>("IncreaseWeight")
                .AddCondition(
                    c => CanStepPreset(WeightPresets, c.WeightMin, c.WeightMax, +1),
                    L.T("body_weight_max_reason", "Weight is already at maximum.")
                )
                .DefaultTooltip(L.T("body_weight_increase_tip", "Increase weight"))
                .ExecuteWith(c => StepWeightPreset(c, +1))
                .Fire(UIEvent.Appearance);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Decrease Build                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Decreases the character's build preset and triggers an appearance update.
        /// </summary>
        public static ControllerAction<WCharacter> DecreaseBuild { get; } =
            Action<WCharacter>("DecreaseBuild")
                .AddCondition(
                    c => CanStepPreset(BuildPresets, c.BuildMin, c.BuildMax, -1),
                    L.T("body_build_min_reason", "Build is already at minimum.")
                )
                .DefaultTooltip(L.T("body_build_decrease_tip", "Decrease build"))
                .ExecuteWith(c => StepBuildPreset(c, -1))
                .Fire(UIEvent.Appearance);

        /// <summary>
        /// Steps the build preset for the character by the given step (-1 or +1).
        /// </summary>
        private static void StepBuildPreset(WCharacter c, int step)
        {
            StepPreset(
                c,
                BuildPresets,
                step,
                () => c.BuildMin,
                v => c.BuildMin = v,
                () => c.BuildMax,
                v => c.BuildMax = v
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Increase Build                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Increases the character's build preset and triggers an appearance update.
        /// </summary>
        public static ControllerAction<WCharacter> IncreaseBuild { get; } =
            Action<WCharacter>("IncreaseBuild")
                .AddCondition(
                    c => CanStepPreset(BuildPresets, c.BuildMin, c.BuildMax, +1),
                    L.T("body_build_max_reason", "Build is already at maximum.")
                )
                .DefaultTooltip(L.T("body_build_increase_tip", "Increase build"))
                .ExecuteWith(c => StepBuildPreset(c, +1))
                .Fire(UIEvent.Appearance);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Shared Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns whether a preset step is valid for the given current min/max and step.
        /// </summary>
        private static bool CanStepPreset(
            List<(float min, float max)> presets,
            float curMin,
            float curMax,
            int step
        )
        {
            if (presets == null || presets.Count == 0)
                return false;

            int idx = NearestPresetIndex(presets, curMin, curMax);
            int next = idx + step;

            return next >= 0 && next < presets.Count;
        }

        /// <summary>
        /// Advances the character's min/max to the preset at the given step and re-applies culture tags.
        /// </summary>
        private static void StepPreset(
            WCharacter c,
            List<(float min, float max)> presets,
            int step,
            Func<float> getMin,
            Action<float> setMin,
            Func<float> getMax,
            Action<float> setMax
        )
        {
            if (c == null || presets == null || presets.Count == 0)
                return;

            try
            {
                float curMin = getMin();
                float curMax = getMax();

                int idx = NearestPresetIndex(presets, curMin, curMax);
                int next = idx + step;

                // Should not happen because ControllerAction conditions gate it,
                // but keep it safe.
                if (next < 0 || next >= presets.Count)
                    return;

                var (min, max) = presets[next];

                setMin(min);
                setMax(max);

                // Re-snap tags using the same culture template selection logic as ApplyCultureBodyProperties,
                // but without touching the whole envelope.
                var template = PickCultureTemplate(c);
                if (template != null)
                    c.ApplyTagsFromCulture(template);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// Finds the index of the nearest preset to the given current min/max.
        /// </summary>
        private static int NearestPresetIndex(
            List<(float min, float max)> presets,
            float curMin,
            float curMax
        )
        {
            int best = 0;
            float bestScore = float.MaxValue;

            for (int i = 0; i < presets.Count; i++)
            {
                var (min, max) = presets[i];
                float score = Math.Abs(min - curMin) + Math.Abs(max - curMax);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>
        /// Picks an appropriate culture template character matching the given character's sex.
        /// </summary>
        private static WCharacter PickCultureTemplate(WCharacter c)
        {
            if (c?.Culture == null)
                return null;

            var template = c.Culture.RootBasic ?? c.Culture.RootElite;

            if (template?.IsFemale != c.IsFemale)
                template = c.IsFemale ? c.Culture.VillageWoman : c.Culture.Villager;

            if (template == null)
            {
                foreach (var troop in c.Culture.Troops)
                {
                    template = troop;
                    if (troop != null && troop.IsFemale == c.IsFemale)
                        break;
                }
            }

            return template;
        }
    }
}
