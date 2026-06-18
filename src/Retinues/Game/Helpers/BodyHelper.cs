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

            // Most main cultures expose BasicTroop/EliteBasicTroop; minor/ancestral cultures (e.g.
            // Nord, Vakken) define neither. Fall back through the culture's other troops, then to
            // the troop's vanilla base, so the switch resets appearance deterministically instead
            // of silently leaving the previous culture's body in place.
            var template = GetCultureTemplate(culture) ?? ResolveVanillaBaseTemplate(troop);
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
#if BL13 || BL14
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
        //                  Template Resolution                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Picks a representative troop to copy body properties / tags from for a culture. Prefers
        /// the culture's basic troop, then falls back through its other troop types and finally any
        /// troop of that culture. Returns null only when the culture has no troops at all (e.g. a
        /// minor/ancestral culture like Nord or Vakken).
        /// </summary>
        internal static CharacterObject GetCultureTemplate(CultureObject culture)
        {
            if (culture == null)
                return null;

            return culture.BasicTroop
                ?? culture.EliteBasicTroop
                ?? culture.MeleeMilitiaTroop
                ?? culture.MeleeEliteMilitiaTroop
                ?? culture.RangedMilitiaTroop
                ?? culture.Villager
                ?? culture.VillageWoman
                ?? culture.CaravanGuard
                ?? FindAnyTroopOfCulture(culture);
        }

        /// <summary>
        /// Scans the object manager for any non-hero troop belonging to the culture that has a body
        /// range to copy. Last-resort fallback for cultures with no standard troop slots filled.
        /// </summary>
        private static CharacterObject FindAnyTroopOfCulture(CultureObject culture)
        {
            try
            {
                foreach (var co in MBObjectManager.Instance.GetObjectTypeList<CharacterObject>())
                    if (
                        co != null
                        && !co.IsHero
                        && co.Culture == culture
                        && co.BodyPropertyRange != null
                    )
                        return co;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            return null;
        }

        /// <summary>
        /// Resolves the troop's vanilla-base CharacterObject (the unit it was cloned from). Used as
        /// a neutral appearance source when the selected culture defines no troops of its own, so
        /// the switch produces a deterministic look instead of inheriting the previous culture.
        /// </summary>
        private static CharacterObject ResolveVanillaBaseTemplate(WCharacter troop)
        {
            var vid = troop?.VanillaStringId;
            if (string.IsNullOrEmpty(vid) || vid == troop.StringId)
                return null;
            return MBObjectManager.Instance.GetObject<CharacterObject>(vid);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Tags Normalization                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ApplyTagsFromCulture(WCharacter troop)
        {
#if BL13 || BL14
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

                // Pick a culture template to pull tags from. Prefer a gender-appropriate template,
                // then the culture's troop fallback chain, then the troop's vanilla base — so even
                // minor/ancestral cultures (e.g. Nord, Vakken) reset tags deterministically.
                CharacterObject template = null;

                if (troop.IsFemale)
                    template = culture.VillageWoman;

                template ??= GetCultureTemplate(culture);
                template ??= ResolveVanillaBaseTemplate(troop);

                if (template == null)
                {
                    Log.Warn(
                        $"[BodyHelper] No usable troop template for culture '{culture.StringId}', aborting."
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
                // Use the template's tag pools as the valid tags for this culture. An empty value
                // means the culture defines none for that category — we still apply it (clearing
                // the target) so switching to a culture with fewer tag categories than the previous
                // one does not leave stale hair/beard/tattoo tags behind.
                var hairTags = templateRange.HairTags ?? string.Empty;
                var beardTags = templateRange.BeardTags ?? string.Empty;
                var tattooTags = templateRange.TattooTags ?? string.Empty;

                // Only rewrite the range when at least one category actually differs.
                bool different =
                    !string.Equals(
                        targetRange.HairTags ?? string.Empty,
                        hairTags,
                        StringComparison.Ordinal
                    )
                    || !string.Equals(
                        targetRange.BeardTags ?? string.Empty,
                        beardTags,
                        StringComparison.Ordinal
                    )
                    || !string.Equals(
                        targetRange.TattooTags ?? string.Empty,
                        tattooTags,
                        StringComparison.Ordinal
                    );

                if (!different)
                    return;

                // Follow vanilla pattern and clone the MBBodyProperty, then overwrite every tag
                // category (including clearing to empty when the culture defines none).
                var clonedRange = MBBodyProperty.CreateFrom(targetRange);
                clonedRange.HairTags = hairTags;
                clonedRange.BeardTags = beardTags;
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
