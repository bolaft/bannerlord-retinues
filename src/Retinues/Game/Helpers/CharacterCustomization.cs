using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Helpers
{
    public static class CharacterCustomization
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Presets                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        
        public static readonly List<(float min, float max)> AgePresets =
        [
            (22,29), (30,49), (50,69), (70,99)
        ];

        public static readonly List<(float min, float max)> WeightPresets =
        [
            (0.01f,0.15f), (0.16f,0.30f), (0.31f,0.55f), (0.56f,0.70f), (0.71f,0.85f), (0.86f,0.99f)
        ];

        public static readonly List<(float min, float max)> BuildPresets =
        [
            (0.01f,0.15f), (0.16f,0.30f), (0.31f,0.55f), (0.56f,0.70f), (0.71f,0.85f), (0.86f,0.99f)
        ];

        public static readonly List<(float min, float max)> HeightPresets =
        [
            (0.01f,0.15f), (0.16f,0.30f), (0.31f,0.55f), (0.56f,0.70f), (0.71f,0.85f), (0.86f,0.99f)
        ];


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Public API                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ApplyNextAgePreset(WCharacter t)    => ApplyPresetStep(t, AgePresets,    +1, () => t.AgeMin,    v => t.AgeMin = v,    () => t.AgeMax,    v => t.AgeMax = v);
        public static void ApplyPrevAgePreset(WCharacter t)    => ApplyPresetStep(t, AgePresets,    -1, () => t.AgeMin,    v => t.AgeMin = v,    () => t.AgeMax,    v => t.AgeMax = v);

        public static void ApplyNextWeightPreset(WCharacter t) => ApplyPresetStep(t, WeightPresets, +1, () => t.WeightMin, v => t.WeightMin = v, () => t.WeightMax, v => t.WeightMax = v);
        public static void ApplyPrevWeightPreset(WCharacter t) => ApplyPresetStep(t, WeightPresets, -1, () => t.WeightMin, v => t.WeightMin = v, () => t.WeightMax, v => t.WeightMax = v);

        public static void ApplyNextBuildPreset(WCharacter t)  => ApplyPresetStep(t, BuildPresets,  +1, () => t.BuildMin,  v => t.BuildMin = v,  () => t.BuildMax,  v => t.BuildMax = v);
        public static void ApplyPrevBuildPreset(WCharacter t)  => ApplyPresetStep(t, BuildPresets,  -1, () => t.BuildMin,  v => t.BuildMin = v,  () => t.BuildMax,  v => t.BuildMax = v);

        public static void ApplyNextHeightPreset(WCharacter t) =>
            ApplyPresetStep(t, HeightPresets, +1, () => t.HeightMin, v => t.HeightMin = v, () => t.HeightMax, v => t.HeightMax = v);
        public static void ApplyPrevHeightPreset(WCharacter t) =>
            ApplyPresetStep(t, HeightPresets, -1, () => t.HeightMin, v => t.HeightMin = v, () => t.HeightMax, v => t.HeightMax = v);

        /* ━━━━━━━━ Helpers ━━━━━━━ */

        private static void ApplyPresetStep(
            WCharacter t,
            List<(float min, float max)> presets,
            int step,
            Func<float> getMin, Action<float> setMin,
            Func<float> getMax, Action<float> setMax)
        {
            if (t == null || presets == null || presets.Count == 0) return;

            float curMin = getMin();
            float curMax = getMax();

            int idx = NearestPresetIndex(presets, curMin, curMax);
            int next = (idx + step) % presets.Count;
            if (next < 0) next += presets.Count;

            var (min, max) = presets[next];
            setMin(min);
            setMax(max);
        }

        private static int NearestPresetIndex(List<(float min, float max)> presets, float curMin, float curMax)
        {
            int best = 0; float bestScore = float.MaxValue;
            for (int i = 0; i < presets.Count; i++)
            {
                var (min, max) = presets[i];
                float score = Math.Abs(min - curMin) + Math.Abs(max - curMax);
                if (score < bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        
        public static void ApplyPropertiesFromCulture(WCharacter troop, CultureObject culture)
        {                
            
            if (culture == null || !troop.IsCustom) return;

            var template = culture.BasicTroop ?? culture.EliteBasicTroop;
            if (template == null) return;

            // break shared reference
            troop.EnsureOwnBodyRange();

            var range = Reflector.GetPropertyValue<object>(troop.Base, "BodyPropertyRange");
            var tplRange = Reflector.GetPropertyValue<object>(template, "BodyPropertyRange");

            // 1) Copy style tags & race (affects FaceGen sampling)
            troop.Race = template.Race;

            // 2) Copy min/max envelope from template
            var min = template.GetBodyPropertiesMin();
            var max = template.GetBodyPropertiesMax();
            Reflector.InvokeMethod(range, "Init", [typeof(BodyProperties), typeof(BodyProperties)], min, max);

            // 4) Snap age to the template's mid-age
            troop.Age = (min.Age + max.Age) * 0.5f;
        }
    }
}
