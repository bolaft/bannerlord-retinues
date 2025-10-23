using System;
using System.Linq;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Game.Helpers
{
    public static class CharacterCustomization
    {
        // ─────────────────────────────────────────────────────────────────────
        // Dynamic caches (lazily built from cultures’ template troops).
        // You can call RebuildTagCaches() explicitly, or they’ll auto-build on first use.
        // ─────────────────────────────────────────────────────────────────────

        private static bool _tagCacheBuilt;
        private static readonly HashSet<string> _hairMale   = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _hairFemale = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _beards     = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _tattoos    = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Scan all cultures and collect hair/beard/tattoo tag strings from their Basic/Elite template troops.</summary>
        public static void RebuildTagCaches()
        {
            _tagCacheBuilt = false;
            _hairMale.Clear(); _hairFemale.Clear(); _beards.Clear(); _tattoos.Clear();

            var cultures = MBObjectManager.Instance.GetObjectTypeList<CultureObject>()?
                .OrderBy(c => c?.Name?.ToString())
                .ToList() ?? new();

            foreach (var c in cultures)
            {
                if (c == null) continue;

                // Basic & Elite templates (some cultures may lack one)
                var templates = new[] { c.BasicTroop, c.EliteBasicTroop }
                    .Where(t => t != null)
                    .Distinct();

                foreach (var t in templates)
                {
                    // Use a lightweight wrapper to read the strings the same way your WCharacter does
                    var tw = new WCharacter(t.StringId);

                    AddIfNotEmpty(tw.HairTags,   t.IsFemale ? _hairFemale : _hairMale);
                    AddIfNotEmpty(tw.BeardTags,  _beards);    // beards mainly matter on males; keep global
                    AddIfNotEmpty(tw.TattooTags, _tattoos);
                }
            }

            _tagCacheBuilt = true;
        }

        private static void EnsureTagCaches()
        {
            if (_tagCacheBuilt) return;
            RebuildTagCaches();
        }

        private static void AddIfNotEmpty(string value, HashSet<string> set)
        {
            if (!string.IsNullOrWhiteSpace(value))
                set.Add(value.Trim());
        }

        /// <summary>Get hair tag options (male/female) collected from cultures.</summary>
        public static List<string> GetHairOptions(bool isFemale)
        {
            EnsureTagCaches();
            var src = isFemale ? _hairFemale : _hairMale;
            // If a gender set is empty (some games have only male templates), fall back to the other.
            return (src.Count > 0 ? src : (isFemale ? _hairMale : _hairFemale)).OrderBy(s => s).ToList();
        }

        public static List<string> GetBeardOptions()
        {
            EnsureTagCaches();
            return _beards.OrderBy(s => s).ToList();
        }

        public static List<string> GetTattooOptions()
        {
            EnsureTagCaches();
            return _tattoos.OrderBy(s => s).ToList();
        }

        // Presets for min/max envelopes (pairs).
        public static readonly List<(float min, float max)> AgePresets =
        [
            (18, 22),
            (20, 28),
            (24, 32),
            (28, 38),
            (35, 45),
            (45, 65),
        ];

        public static readonly List<(float min, float max)> WeightPresets =
        [
            (0.40f, 0.50f),
            (0.45f, 0.55f),
            (0.50f, 0.60f),
            (0.55f, 0.65f),
        ];

        public static readonly List<(float min, float max)> BuildPresets =
        [
            (0.40f, 0.50f),
            (0.45f, 0.55f),
            (0.50f, 0.60f),
            (0.55f, 0.65f),
        ];

        // ─────────────────────────────────────────────────────────────────────
        // Discrete cycling: Hair / Beard / Tattoo
        // ─────────────────────────────────────────────────────────────────────

        public static void ApplyNextHair(WCharacter troop, bool isFemale) =>
            troop.HairTags = CycleNext(troop.HairTags, GetHairOptions(isFemale));

        public static void ApplyPrevHair(WCharacter troop, bool isFemale) =>
            troop.HairTags = CyclePrev(troop.HairTags, GetHairOptions(isFemale));

        public static void ApplyNextBeard(WCharacter troop) =>
            troop.BeardTags = CycleNext(troop.BeardTags, GetBeardOptions());

        public static void ApplyPrevBeard(WCharacter troop) =>
            troop.BeardTags = CyclePrev(troop.BeardTags, GetBeardOptions());

        public static void ApplyNextTattoo(WCharacter troop) =>
            troop.TattooTags = CycleNext(troop.TattooTags, GetTattooOptions());

        public static void ApplyPrevTattoo(WCharacter troop) =>
            troop.TattooTags = CyclePrev(troop.TattooTags, GetTattooOptions());

        // ─────────────────────────────────────────────────────────────────────
        // Continuous nudges (±10% of “base”): Age / Weight / Build
        // We nudge the current envelope by ±10% of the current center for that stat.
        // ─────────────────────────────────────────────────────────────────────

        // Increase / decrease (±10%) — now using getter/setter delegates
        public static void ApplyIncreaseAge(WCharacter t) =>
            NudgeEnvelope(
                t,
                () => t.AgeMin,
                v => t.AgeMin = v,
                () => t.AgeMax,
                v => t.AgeMax = v,
                (14f, 90f),
                +0.10f
            );

        public static void ApplyDecreaseAge(WCharacter t) =>
            NudgeEnvelope(
                t,
                () => t.AgeMin,
                v => t.AgeMin = v,
                () => t.AgeMax,
                v => t.AgeMax = v,
                (14f, 90f),
                -0.10f
            );

        public static void ApplyIncreaseWeight(WCharacter t) =>
            NudgeEnvelope(
                t,
                () => t.WeightMin,
                v => t.WeightMin = v,
                () => t.WeightMax,
                v => t.WeightMax = v,
                (0f, 1f),
                +0.10f
            );

        public static void ApplyDecreaseWeight(WCharacter t) =>
            NudgeEnvelope(
                t,
                () => t.WeightMin,
                v => t.WeightMin = v,
                () => t.WeightMax,
                v => t.WeightMax = v,
                (0f, 1f),
                -0.10f
            );

        public static void ApplyIncreaseBuild(WCharacter t) =>
            NudgeEnvelope(
                t,
                () => t.BuildMin,
                v => t.BuildMin = v,
                () => t.BuildMax,
                v => t.BuildMax = v,
                (0f, 1f),
                +0.10f
            );

        public static void ApplyDecreaseBuild(WCharacter t) =>
            NudgeEnvelope(
                t,
                () => t.BuildMin,
                v => t.BuildMin = v,
                () => t.BuildMax,
                v => t.BuildMax = v,
                (0f, 1f),
                -0.10f
            );

        // Core nudger: reads current min/max via funcs, computes new values, writes back
        private static void NudgeEnvelope(
            WCharacter t,
            Func<float> getMin,
            Action<float> setMin,
            Func<float> getMax,
            Action<float> setMax,
            (float lo, float hi) clamp,
            float pct
        )
        {
            if (t == null)
                return;

            float min = getMin();
            float max = getMax();

            float center = 0.5f * (min + max);
            float delta = Math.Abs(center) * Math.Abs(pct);

            // tiny fallback if center is ~0 (e.g., weight/build)
            if (delta <= 1e-6f)
                delta = (clamp.hi - clamp.lo) * Math.Abs(pct) * 0.05f;

            float step = Math.Sign(pct) * delta * 0.5f; // split half to each side

            min = Clamp(min + step, clamp.lo, clamp.hi);
            max = Clamp(max + step, clamp.lo, clamp.hi);

            if (min > max)
                (min, max) = (max, min);

            setMin(min);
            setMax(max);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Preset cycling for min/max pairs: Age / Weight / Build
        // We pick the nearest current preset, then move ±1 (wrap-around).
        // ─────────────────────────────────────────────────────────────────────

        public static void ApplyNextAgePreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                AgePresets,
                +1,
                set: (t, v) =>
                {
                    t.AgeMin = v.min;
                    t.AgeMax = v.max;
                }
            );

        public static void ApplyPrevAgePreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                AgePresets,
                -1,
                set: (t, v) =>
                {
                    t.AgeMin = v.min;
                    t.AgeMax = v.max;
                }
            );

        public static void ApplyNextWeightPreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                WeightPresets,
                +1,
                set: (t, v) =>
                {
                    t.WeightMin = v.min;
                    t.WeightMax = v.max;
                }
            );

        public static void ApplyPrevWeightPreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                WeightPresets,
                -1,
                set: (t, v) =>
                {
                    t.WeightMin = v.min;
                    t.WeightMax = v.max;
                }
            );

        public static void ApplyNextBuildPreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                BuildPresets,
                +1,
                set: (t, v) =>
                {
                    t.BuildMin = v.min;
                    t.BuildMax = v.max;
                }
            );

        public static void ApplyPrevBuildPreset(WCharacter t) =>
            ApplyPresetStep(
                t,
                BuildPresets,
                -1,
                set: (t, v) =>
                {
                    t.BuildMin = v.min;
                    t.BuildMax = v.max;
                }
            );

        // ─────────────────────────────────────────────────────────────────────
        // Internals
        // ─────────────────────────────────────────────────────────────────────

        private static string CycleNext(string current, List<string> options)
        {
            if (options == null || options.Count == 0)
                return current ?? string.Empty;
            var idx = options.FindIndex(s =>
                string.Equals(s ?? "", current ?? "", StringComparison.OrdinalIgnoreCase)
            );
            return options[(idx + 1 + options.Count) % options.Count];
        }

        private static string CyclePrev(string current, List<string> options)
        {
            if (options == null || options.Count == 0)
                return current ?? string.Empty;
            var idx = options.FindIndex(s =>
                string.Equals(s ?? "", current ?? "", StringComparison.OrdinalIgnoreCase)
            );
            if (idx < 0)
                idx = 0;
            return options[(idx - 1 + options.Count) % options.Count];
        }

        private static void ApplyPresetStep(
            WCharacter t,
            List<(float min, float max)> presets,
            int step,
            Action<WCharacter, (float min, float max)> set
        )
        {
            if (presets == null || presets.Count == 0)
                return;

            // Current as (min,max)
            (float curMin, float curMax) cur =
                (presets == AgePresets) ? (t.AgeMin, t.AgeMax)
                : (presets == WeightPresets) ? (t.WeightMin, t.WeightMax)
                : (t.BuildMin, t.BuildMax);

            var idx = NearestPresetIndex(presets, cur.curMin, cur.curMax);
            var next = (idx + step % presets.Count + presets.Count) % presets.Count;
            set(t, presets[next]);
        }

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
                var p = presets[i];
                // distance: L1 on min/max + center diff weight
                float score = Math.Abs(p.min - curMin) + Math.Abs(p.max - curMax);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>
        /// Copy visuals from a culture template (Basic → Elite fallback) with optional spreads.
        /// </summary>
        public static void ApplyPropertiesFromCulture(
            WCharacter troop,
            CultureObject culture,
            bool copyRace = true,
            bool copyTags = true,
            // Set the min/max envelope from template, then optionally widen with spreads:
            bool setEnvelopeFromTemplate = true,
            float ageSpread = 0f, // total span (years) to add around the center
            float weightSpread = 0f, // total span in 0..1
            float buildSpread = 0f, // total span in 0..1
            int? seed = null
        )
        {
            if (troop == null || culture == null || troop.IsHero)
                return;

            var tpl = culture.BasicTroop ?? culture.EliteBasicTroop;
            if (tpl == null)
                return;

            if (copyRace)
                troop.Race = tpl.Race;

            if (copyTags)
            {
                // Read template tags; on BL 1.3.x they live on BodyPropertyRange
                var tplW = new WCharacter(tpl.StringId);
                troop.HairTags = tplW.HairTags;
                troop.BeardTags = tplW.BeardTags;
                troop.TattooTags = tplW.TattooTags;
            }

            // Base envelope from the template
            if (setEnvelopeFromTemplate)
            {
                var tMin = tpl.GetBodyPropertiesMin();
                var tMax = tpl.GetBodyPropertiesMax();

                troop.AgeMin = tMin.Age;
                troop.AgeMax = tMax.Age;
                troop.WeightMin = tMin.Weight;
                troop.WeightMax = tMax.Weight;
                troop.BuildMin = tMin.Build;
                troop.BuildMax = tMax.Build;

                // If no spread requested, we're done.
                if (ageSpread <= 0f && weightSpread <= 0f && buildSpread <= 0f)
                    return;
            }

            // Widen envelope with an asymmetric, deterministic skew (optional)
            var rng = MakeRng(seed);
            var skewA = NextSkew(rng);
            var skewW = NextSkew(rng);
            var skewB = NextSkew(rng);

            // Center = current mid; spread = requested total span
            var ageCenter = 0.5f * (troop.AgeMin + troop.AgeMax);
            var weightCenter = 0.5f * (troop.WeightMin + troop.WeightMax);
            var buildCenter = 0.5f * (troop.BuildMin + troop.BuildMax);

            var (ageMin, ageMax) = Spread(ageCenter, ageSpread, skewA, 14f, 90f);
            var (weightMin, weightMax) = Spread(weightCenter, weightSpread, skewW, 0f, 1f);
            var (buildMin, buildMax) = Spread(buildCenter, buildSpread, skewB, 0f, 1f);

            troop.AgeMin = ageMin;
            troop.AgeMax = ageMax;
            troop.WeightMin = weightMin;
            troop.WeightMax = weightMax;
            troop.BuildMin = buildMin;
            troop.BuildMax = buildMax;
        }

        // helpers
        private static (float min, float max) Spread(
            float center,
            float span,
            float skew,
            float lo,
            float hi
        )
        {
            if (span <= 0f)
                return (Clamp(center, lo, hi), Clamp(center, lo, hi));
            float half = 0.5f * span;
            float left = half * (1f - skew); // skew in [-0.5, +0.5] makes asymmetry
            float right = span - left;
            return (Clamp(center - left, lo, hi), Clamp(center + right, lo, hi));
        }

        private static float Clamp(float v, float a, float b) => v < a ? a : (v > b ? b : v);

        private static Random MakeRng(int? seed) =>
            seed.HasValue ? new Random(seed.Value ^ unchecked((int)0x9E3779B9)) : new Random();

        private static float NextSkew(Random r) => (float)(r.NextDouble() - 0.5); // [-0.5, +0.5]
    }
}
