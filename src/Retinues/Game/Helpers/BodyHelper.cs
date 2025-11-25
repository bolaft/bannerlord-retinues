using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Helpers
{
    [SafeClass]
    public static class BodyHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Presets                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static readonly List<(float min, float max)> AgePresets =
        [
            (22, 29),
            (30, 49),
            (50, 69),
            (70, 99),
        ];

        public static readonly List<(float min, float max)> WeightPresets =
        [
            (0.01f, 0.15f),
            (0.16f, 0.30f),
            (0.31f, 0.55f),
            (0.56f, 0.70f),
            (0.71f, 0.85f),
            (0.86f, 0.99f),
        ];

        public static readonly List<(float min, float max)> BuildPresets =
        [
            (0.01f, 0.15f),
            (0.16f, 0.30f),
            (0.31f, 0.55f),
            (0.56f, 0.70f),
            (0.71f, 0.85f),
            (0.86f, 0.99f),
        ];

        public static readonly List<(float min, float max)> HeightPresets =
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

        public static void ApplyNextAgePreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                AgePresets,
                +1,
                () => t.Body.AgeMin,
                v => t.Body.AgeMin = v,
                () => t.Body.AgeMax,
                v => t.Body.AgeMax = v
            );

        public static void ApplyPrevAgePreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                AgePresets,
                -1,
                () => t.Body.AgeMin,
                v => t.Body.AgeMin = v,
                () => t.Body.AgeMax,
                v => t.Body.AgeMax = v
            );

        public static void ApplyNextWeightPreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                WeightPresets,
                +1,
                () => t.Body.WeightMin,
                v => t.Body.WeightMin = v,
                () => t.Body.WeightMax,
                v => t.Body.WeightMax = v
            );

        public static void ApplyPrevWeightPreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                WeightPresets,
                -1,
                () => t.Body.WeightMin,
                v => t.Body.WeightMin = v,
                () => t.Body.WeightMax,
                v => t.Body.WeightMax = v
            );

        public static void ApplyNextBuildPreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                BuildPresets,
                +1,
                () => t.Body.BuildMin,
                v => t.Body.BuildMin = v,
                () => t.Body.BuildMax,
                v => t.Body.BuildMax = v
            );

        public static void ApplyPrevBuildPreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                BuildPresets,
                -1,
                () => t.Body.BuildMin,
                v => t.Body.BuildMin = v,
                () => t.Body.BuildMax,
                v => t.Body.BuildMax = v
            );

        public static void ApplyNextHeightPreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                HeightPresets,
                +1,
                () => t.Body.HeightMin,
                v => t.Body.HeightMin = v,
                () => t.Body.HeightMax,
                v => t.Body.HeightMax = v
            );

        public static void ApplyPrevHeightPreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                HeightPresets,
                -1,
                () => t.Body.HeightMin,
                v => t.Body.HeightMin = v,
                () => t.Body.HeightMax,
                v => t.Body.HeightMax = v
            );

        /* ━━━━━━━━ Helpers ━━━━━━━ */

        /// <summary>
        /// Applies the preset step to the given troop and property getters/setters.
        /// </summary>
        private static void ApplyPresetStep(
            WCharacter t,
            List<(float min, float max)> presets,
            int step,
            Func<float> getMin,
            Action<float> setMin,
            Func<float> getMax,
            Action<float> setMax
        )
        {
            if (t == null || presets == null || presets.Count == 0)
                return;

            float curMin = getMin();
            float curMax = getMax();

            int idx = NearestPresetIndex(presets, curMin, curMax);
            int next = (idx + step) % presets.Count;
            if (next < 0)
                next += presets.Count;

            var (min, max) = presets[next];
            setMin(min);
            setMax(max);

            ApplyTagsFromCulture(t);
        }

        /// <summary>
        /// Finds the index of the preset closest to the given min/max values.
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Applies body properties from the given culture to the specified troop.
        /// </summary>
        public static void ApplyPropertiesFromCulture(WCharacter troop, CultureObject culture)
        {
            if (troop == null || culture == null)
                return;

            var template = culture.BasicTroop ?? culture.EliteBasicTroop;
            if (template == null)
                return;

            // Hero path: edit Hero.BodyProperties instead of template range
            var hero = troop.Base?.HeroObject;
            if (hero != null)
            {
                try
                {
                    // 1) Take the template's min/max envelope
                    var min = template.GetBodyPropertiesMin();
                    var max = template.GetBodyPropertiesMax();

                    // 2) Compute a "mid" dynamic body (age/weight/build)
                    float midAge = (min.Age + max.Age) * 0.5f;
                    float midWeight = (min.Weight + max.Weight) * 0.5f;
                    float midBuild = (min.Build + max.Build) * 0.5f;

                    var dyn = new DynamicBodyProperties(midAge, midWeight, midBuild);

                    // 3) Use the template's static properties (includes race/face style etc.)
                    var stat = min.StaticProperties;
                    var newBody = new BodyProperties(dyn, stat);

                    // 4) Apply to the hero
#if BL13
                    hero.StaticBodyProperties = newBody.StaticProperties;
#else
                    Reflector.SetPropertyValue(
                        hero,
                        "StaticBodyProperties",
                        newBody.StaticProperties
                    );
#endif
                    // Age is derived from birthday
                    hero.SetBirthDay(CampaignTime.YearsFromNow(-midAge));

                    // Optional: keep internal flag for save/export
                    troop.NeedsPersistence = true;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }

                return;
            }

            // Troop path (non-hero): keep existing template-based logic

            // break shared reference
            troop.Body.EnsureOwnBodyRange();

            var range = Reflector.GetPropertyValue<object>(troop.Base, "BodyPropertyRange");

            // 1) Copy style tags & race (affects FaceGen sampling)
            troop.Race = template.Race;

            // 2) Copy min/max envelope from template
            var minTroop = template.GetBodyPropertiesMin();
            var maxTroop = template.GetBodyPropertiesMax();
            Reflector.InvokeMethod(
                range,
                "Init",
                [typeof(BodyProperties), typeof(BodyProperties)],
                minTroop,
                maxTroop
            );

            // 4) Snap age to the template's mid-age
            troop.Body.Age = (minTroop.Age + maxTroop.Age) * 0.5f;

            // 5) Re-snap hair/scar/tattoo tags from culture template
            ApplyTagsFromCulture(troop);
        }

        /// <summary>
        /// Applies body properties from the given culture id to the specified troop.
        /// </summary>
        public static void ApplyPropertiesFromCulture(WCharacter troop, string cultureId)
        {
            var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
            ApplyPropertiesFromCulture(troop, culture);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Tags Normalization                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ApplyTagsFromCulture(WCharacter troop)
        {
#if BL13
            try
            {
                var character = troop?.Base;
                var culture = troop?.Culture.Base;

                if (character == null || culture == null)
                    return;

                // CharacterObject inherits BasicCharacterObject, so this cast is always valid.
                if (character is not BasicCharacterObject basic)
                    return;

                // Heroes use Hero.StaticBodyProperties, let that pipeline handle them.
                if (basic.IsHero)
                    return;

                // Pick a culture template to pull tags from.
                var template = culture.BasicTroop ?? culture.EliteBasicTroop;
                if (template == null)
                {
                    Log.Warn(
                        $"[BodyHelper] No BasicTroop/EliteBasicTroop for culture '{culture.StringId}', aborting."
                    );
                    return;
                }

                var templateRange = template.BodyPropertyRange;
                var targetRange = basic.BodyPropertyRange;

                if (templateRange == null || targetRange == null)
                {
                    Log.Warn(
                        "[BodyHelper] Missing BodyPropertyRange on template or target, aborting."
                    );
                    return;
                }
                // Use the template's tag pools as "valid" tags for this culture.
                var hairTags = templateRange.HairTags ?? string.Empty;
                var beardTags = templateRange.BeardTags ?? string.Empty;
                var tattooTags = templateRange.TattooTags ?? string.Empty;

                bool hasHair = !string.IsNullOrEmpty(hairTags);
                bool hasBeard = !string.IsNullOrEmpty(beardTags);
                bool hasTattoo = !string.IsNullOrEmpty(tattooTags);

                // Nothing to apply, bail out.
                if (!hasHair && !hasBeard && !hasTattoo)
                {
                    Log.Info("[BodyHelper] Template has no tags, nothing to apply.");
                    return;
                }

                // Check if we actually need to change anything.
                bool different =
                    (
                        hasHair
                        && !string.Equals(targetRange.HairTags, hairTags, StringComparison.Ordinal)
                    )
                    || (
                        hasBeard
                        && !string.Equals(
                            targetRange.BeardTags,
                            beardTags,
                            StringComparison.Ordinal
                        )
                    )
                    || (
                        hasTattoo
                        && !string.Equals(
                            targetRange.TattooTags,
                            tattooTags,
                            StringComparison.Ordinal
                        )
                    );

                if (!different)
                    return;

                // Follow vanilla pattern and clone the MBBodyProperty.
                var clonedRange = MBBodyProperty.CreateFrom(targetRange);

                if (hasHair)
                    clonedRange.HairTags = hairTags;

                if (hasBeard)
                    clonedRange.BeardTags = beardTags;

                if (hasTattoo)
                    clonedRange.TattooTags = tattooTags;

                // Assign back via reflection because BodyPropertyRange setter is protected.
                Reflector.SetPropertyValue(basic, "BodyPropertyRange", clonedRange);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
#endif
        }
    }
}
