using System;
using System.Collections.Generic;
using Retinues.Model.Characters;
using Retinues.Utilities;

namespace Retinues.Editor.Controllers
{
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
        //                        Public API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void PrevAgePreset() =>
            ApplyPresetStep(
                AgePresets,
                -1,
                () => State.Character.AgeMin,
                v => State.Character.AgeMin = v,
                () => State.Character.AgeMax,
                v => State.Character.AgeMax = v
            );

        public static void NextAgePreset() =>
            ApplyPresetStep(
                AgePresets,
                +1,
                () => State.Character.AgeMin,
                v => State.Character.AgeMin = v,
                () => State.Character.AgeMax,
                v => State.Character.AgeMax = v
            );

        public static void PrevWeightPreset() =>
            ApplyPresetStep(
                WeightPresets,
                -1,
                () => State.Character.WeightMin,
                v => State.Character.WeightMin = v,
                () => State.Character.WeightMax,
                v => State.Character.WeightMax = v
            );

        public static void NextWeightPreset() =>
            ApplyPresetStep(
                WeightPresets,
                +1,
                () => State.Character.WeightMin,
                v => State.Character.WeightMin = v,
                () => State.Character.WeightMax,
                v => State.Character.WeightMax = v
            );

        public static void PrevBuildPreset() =>
            ApplyPresetStep(
                BuildPresets,
                -1,
                () => State.Character.BuildMin,
                v => State.Character.BuildMin = v,
                () => State.Character.BuildMax,
                v => State.Character.BuildMax = v
            );

        public static void NextBuildPreset() =>
            ApplyPresetStep(
                BuildPresets,
                +1,
                () => State.Character.BuildMin,
                v => State.Character.BuildMin = v,
                () => State.Character.BuildMax,
                v => State.Character.BuildMax = v
            );

        public static void PrevHeightPreset() =>
            ApplyPresetStep(
                HeightPresets,
                -1,
                () => State.Character.HeightMin,
                v => State.Character.HeightMin = v,
                () => State.Character.HeightMax,
                v => State.Character.HeightMax = v
            );

        public static void NextHeightPreset() =>
            ApplyPresetStep(
                HeightPresets,
                +1,
                () => State.Character.HeightMin,
                v => State.Character.HeightMin = v,
                () => State.Character.HeightMax,
                v => State.Character.HeightMax = v
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static void ApplyPresetStep(
            List<(float min, float max)> presets,
            int step,
            Func<float> getMin,
            Action<float> setMin,
            Func<float> getMax,
            Action<float> setMax
        )
        {
            var c = State.Character;
            if (c == null || presets == null || presets.Count == 0)
                return;

            try
            {
                float curMin = getMin();
                float curMax = getMax();

                int idx = NearestPresetIndex(presets, curMin, curMax);
                int next = (idx + step) % presets.Count;
                if (next < 0)
                    next += presets.Count;

                var (min, max) = presets[next];

                setMin(min);
                setMax(max);

                // Re-snap tags using the same culture template selection logic as ApplyCultureBodyProperties,
                // but without touching the whole envelope.
                var template = PickCultureTemplate(c);
                if (template != null)
                    c.ApplyTagsFromCulture(template);

                // Notify UI/tableau.
                // If your event name differs, change this single line.
                EventManager.Fire(UIEvent.Appearance, EventScope.Local);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        static int NearestPresetIndex(
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

        static WCharacter PickCultureTemplate(WCharacter c)
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
