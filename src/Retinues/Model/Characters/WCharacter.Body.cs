using System;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Visuals                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Gender ━━━━━━━━ */

        MAttribute<bool> IsFemaleAttribute => Attribute<bool>(nameof(CharacterObject.IsFemale));

        public bool IsFemale
        {
            get => IsFemaleAttribute.Get();
            set => IsFemaleAttribute.Set(value);
        }

        /* ━━━━━━━━━ Race ━━━━━━━━━ */

        MAttribute<int> RaceAttribute => Attribute<int>(nameof(CharacterObject.Race));

        public int Race
        {
            get => RaceAttribute.Get();
            set => RaceAttribute.Set(value);
        }

        /* ━━━━━━━━━━ Age ━━━━━━━━━ */

        MAttribute<float> AgeAttribute => Attribute<float>(nameof(CharacterObject.Age));

        public float Age
        {
            get => AgeAttribute.Get();
            set => AgeAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void ApplyCultureBodyProperties()
        {
            var template = Culture.RootBasic ?? Culture.RootElite;

            if (template?.IsFemale != IsFemale)
                template = IsFemale ? Culture.VillageWoman : Culture.Villager;

            if (template == null)
            {
                foreach (var troop in Culture.Troops)
                {
                    template = troop;
                    if (troop.IsFemale == IsFemale)
                        break;
                }
            }

            if (template == null)
            {
                Log.Warn($"No suitable template for culture '{Culture.StringId}', aborting.");
                return;
            }

            // Break shared reference
            EnsureOwnBodyRange();

            var range = Reflection.GetPropertyValue<object>(Base, "BodyPropertyRange");

            // 1) Copy race.
            Race = template.Race;

            // 2) Copy min/max envelope from template.
            var minTroop = template.Base.GetBodyPropertiesMin();
            var maxTroop = template.Base.GetBodyPropertiesMax();
            Reflection.InvokeMethod(
                range,
                "Init",
                [typeof(BodyProperties), typeof(BodyProperties)],
                minTroop,
                maxTroop
            );

            // 4) Snap age to the template's mid-age
            Age = (minTroop.Age + maxTroop.Age) * 0.5f;

            // 5) Re-snap hair/scar/tattoo tags from culture template
            ApplyTagsFromCulture(template);
        }

        public void ApplyTagsFromCulture(WCharacter template)
        {
#if BL13
            var templateRange = template.Base.BodyPropertyRange;
            var range = Base.BodyPropertyRange;

            if (templateRange == null || range == null)
            {
                Log.Warn("Missing BodyPropertyRange on template or target, aborting.");
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
                Log.Info("Template has no tags, nothing to apply.");
                return;
            }

            // Check if we actually need to change anything.
            bool different =
                (hasHair && !string.Equals(range.HairTags, hairTags, StringComparison.Ordinal))
                || (
                    hasBeard && !string.Equals(range.BeardTags, beardTags, StringComparison.Ordinal)
                )
                || (
                    hasTattoo
                    && !string.Equals(range.TattooTags, tattooTags, StringComparison.Ordinal)
                );

            if (!different)
                return;

            if (hasHair)
                HairTags = hairTags;

            if (hasBeard)
                BeardTags = beardTags;

            if (hasTattoo)
                TattooTags = tattooTags;
#endif
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Height Range                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Height is NOT a DynamicBodyProperties channel (unlike age/weight/build).
        // It is encoded in StaticBodyProperties key parts (bitfield).

        const int HEIGHT_PART = 8;
        const int HEIGHT_START = 19;
        const int HEIGHT_BITS = 6;

        public void SetHeightRange(float minHeight, float maxHeight)
        {
            HeightMin = minHeight;
            HeightMax = maxHeight;
        }

        public float HeightMin
        {
            get => ReadHeight(minEnd: true);
            set => SetHeightEnd(minEnd: true, value01: value);
        }

        public float HeightMax
        {
            get => ReadHeight(minEnd: false);
            set => SetHeightEnd(minEnd: false, value01: value);
        }

        float ReadHeight(bool minEnd)
        {
            var bp = minEnd ? Base.GetBodyPropertiesMin() : Base.GetBodyPropertiesMax();
            var sp = bp.StaticProperties;

            ulong part = GetKeyPart(sp, HEIGHT_PART);
            int raw = GetBitsValueFromKey(part, HEIGHT_START, HEIGHT_BITS);

            int max = (1 << HEIGHT_BITS) - 1;
            return max > 0 ? raw / (float)max : 0f;
        }

        void SetHeightEnd(bool minEnd, float value01) =>
            SetStaticChannelEnd(minEnd, HEIGHT_PART, HEIGHT_START, HEIGHT_BITS, value01);

        void SetStaticChannelEnd(bool minEnd, int partIdx, int startBit, int numBits, float value01)
        {
            var range = EnsureOwnBodyRange();
            if (range == null)
                return;

            float v = Clamp01(value01);
            int raw = (int)Math.Round(v * ((1 << numBits) - 1));

            var min = range.BodyPropertyMin;
            var max = range.BodyPropertyMax;

            var src = minEnd ? min : max;
            var oth = minEnd ? max : min;

            var sp = src.StaticProperties;
            ulong part = GetKeyPart(sp, partIdx);

            part = SetBits(part, startBit, numBits, raw);

            var newSp = SetKeyPart(sp, partIdx, part);

            var newSrc = new BodyProperties(src.DynamicProperties, newSp);

            var newMin = minEnd ? newSrc : oth;
            var newMax = minEnd ? oth : newSrc;

            range.Init(newMin, newMax);
        }

        static float Clamp01(float v)
        {
            if (v < 0f)
                return 0f;
            if (v > 1f)
                return 1f;
            return v;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Static KeyPart helpers               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static int GetBitsValueFromKey(ulong part, int startBit, int numBits)
        {
            ulong shifted = part >> startBit;
            ulong mask = (1UL << numBits) - 1UL;
            return (int)(shifted & mask);
        }

        static ulong SetBits(ulong part, int startBit, int numBits, int newValue)
        {
            ulong mask = ((1UL << numBits) - 1UL) << startBit;
            return (part & ~mask) | ((ulong)newValue << startBit);
        }

        static ulong GetKeyPart(in StaticBodyProperties sp, int idx) =>
            idx switch
            {
                1 => sp.KeyPart1,
                2 => sp.KeyPart2,
                3 => sp.KeyPart3,
                4 => sp.KeyPart4,
                5 => sp.KeyPart5,
                6 => sp.KeyPart6,
                7 => sp.KeyPart7,
                _ => sp.KeyPart8,
            };

        static StaticBodyProperties SetKeyPart(in StaticBodyProperties sp, int idx, ulong val) =>
            idx switch
            {
                1 => new(
                    val,
                    sp.KeyPart2,
                    sp.KeyPart3,
                    sp.KeyPart4,
                    sp.KeyPart5,
                    sp.KeyPart6,
                    sp.KeyPart7,
                    sp.KeyPart8
                ),
                2 => new(
                    sp.KeyPart1,
                    val,
                    sp.KeyPart3,
                    sp.KeyPart4,
                    sp.KeyPart5,
                    sp.KeyPart6,
                    sp.KeyPart7,
                    sp.KeyPart8
                ),
                3 => new(
                    sp.KeyPart1,
                    sp.KeyPart2,
                    val,
                    sp.KeyPart4,
                    sp.KeyPart5,
                    sp.KeyPart6,
                    sp.KeyPart7,
                    sp.KeyPart8
                ),
                4 => new(
                    sp.KeyPart1,
                    sp.KeyPart2,
                    sp.KeyPart3,
                    val,
                    sp.KeyPart5,
                    sp.KeyPart6,
                    sp.KeyPart7,
                    sp.KeyPart8
                ),
                5 => new(
                    sp.KeyPart1,
                    sp.KeyPart2,
                    sp.KeyPart3,
                    sp.KeyPart4,
                    val,
                    sp.KeyPart6,
                    sp.KeyPart7,
                    sp.KeyPart8
                ),
                6 => new(
                    sp.KeyPart1,
                    sp.KeyPart2,
                    sp.KeyPart3,
                    sp.KeyPart4,
                    sp.KeyPart5,
                    val,
                    sp.KeyPart7,
                    sp.KeyPart8
                ),
                7 => new(
                    sp.KeyPart1,
                    sp.KeyPart2,
                    sp.KeyPart3,
                    sp.KeyPart4,
                    sp.KeyPart5,
                    sp.KeyPart6,
                    val,
                    sp.KeyPart8
                ),
                _ => new(
                    sp.KeyPart1,
                    sp.KeyPart2,
                    sp.KeyPart3,
                    sp.KeyPart4,
                    sp.KeyPart5,
                    sp.KeyPart6,
                    sp.KeyPart7,
                    val
                ),
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Age Range                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void SetAgeRange(float minAge, float maxAge)
        {
            AgeMin = minAge;
            AgeMax = maxAge;
        }

        public float AgeMin
        {
            get => Base.GetBodyPropertiesMin().Age;
            set => SetBodyDynamicEnd(minEnd: true, age: value, weight: null, build: null);
        }

        public float AgeMax
        {
            get => Base.GetBodyPropertiesMax().Age;
            set => SetBodyDynamicEnd(minEnd: false, age: value, weight: null, build: null);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Weight Range                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void SetWeightRange(float minWeight, float maxWeight)
        {
            WeightMin = minWeight;
            WeightMax = maxWeight;
        }

        public float WeightMin
        {
            get => Base.GetBodyPropertiesMin().Weight;
            set => SetBodyDynamicEnd(minEnd: true, age: null, weight: value, build: null);
        }

        public float WeightMax
        {
            get => Base.GetBodyPropertiesMax().Weight;
            set => SetBodyDynamicEnd(minEnd: false, age: null, weight: value, build: null);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Build Range                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void SetBuildRange(float minBuild, float maxBuild)
        {
            BuildMin = minBuild;
            BuildMax = maxBuild;
        }

        public float BuildMin
        {
            get => Base.GetBodyPropertiesMin().Build;
            set => SetBodyDynamicEnd(minEnd: true, age: null, weight: null, build: value);
        }

        public float BuildMax
        {
            get => Base.GetBodyPropertiesMax().Build;
            set => SetBodyDynamicEnd(minEnd: false, age: null, weight: null, build: value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Tags                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ Hair Tags ━━━━━━ */

        MAttribute<string> HairTagsAttribute =>
            Attribute(
                getter: _ => Base.BodyPropertyRange.HairTags,
                setter: (_, value) =>
                {
                    var clonedRange = MBBodyProperty.CreateFrom(Base.BodyPropertyRange);
                    clonedRange.HairTags = value ?? string.Empty;
                    Reflection.SetPropertyValue(Base, "BodyPropertyRange", clonedRange);
                }
            );

        public string HairTags
        {
            get => HairTagsAttribute.Get();
            set => HairTagsAttribute.Set(value);
        }

        /* ━━━━━━ Beard Tags ━━━━━━ */

        MAttribute<string> BeardTagsAttribute =>
            Attribute(
                getter: (_) => Base.BodyPropertyRange.BeardTags,
                setter: (_, value) =>
                {
                    var clonedRange = MBBodyProperty.CreateFrom(Base.BodyPropertyRange);
                    clonedRange.BeardTags = value ?? string.Empty;
                    Reflection.SetPropertyValue(Base, "BodyPropertyRange", clonedRange);
                }
            );

        public string BeardTags
        {
            get => BeardTagsAttribute.Get();
            set => BeardTagsAttribute.Set(value);
        }

        /* ━━━━━━ Tattoo Tags ━━━━━ */

        MAttribute<string> TattooTagsAttribute =>
            Attribute(
                getter: _ => Base.BodyPropertyRange.TattooTags,
                setter: (_, value) =>
                {
                    var clonedRange = MBBodyProperty.CreateFrom(Base.BodyPropertyRange);
                    clonedRange.TattooTags = value ?? string.Empty;
                    Reflection.SetPropertyValue(Base, "BodyPropertyRange", clonedRange);
                }
            );

        public string TattooTags
        {
            get => TattooTagsAttribute.Get();
            set => TattooTagsAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MBBodyProperty GetBodyRangeOrNull() =>
            Reflection.GetPropertyValue<MBBodyProperty>(Base, "BodyPropertyRange");

        void SetBodyDynamicEnd(bool minEnd, float? age, float? weight, float? build)
        {
            var range = EnsureOwnBodyRange();
            if (range == null)
                return;

            var min = range.BodyPropertyMin;
            var max = range.BodyPropertyMax;

            var src = minEnd ? min : max;
            var oth = minEnd ? max : min;

            var dyn = src.DynamicProperties;

            var newDyn = new DynamicBodyProperties(
                age ?? dyn.Age,
                weight ?? dyn.Weight,
                build ?? dyn.Build
            );

            var newSrc = new BodyProperties(newDyn, src.StaticProperties);

            var newMin = minEnd ? newSrc : oth;
            var newMax = minEnd ? oth : newSrc;

            range.Init(newMin, newMax);
        }

        MBBodyProperty EnsureOwnBodyRange()
        {
            var current = GetBodyRangeOrNull();
            if (current == null)
            {
                var min = Base.GetBodyPropertiesMin();
                var max = Base.GetBodyPropertiesMax();

                var mbBodyType = typeof(BodyProperties).Assembly.GetType(
                    "TaleWorlds.Core.MBBodyProperty"
                );

                if (mbBodyType == null)
                    return null;

                var fresh = (MBBodyProperty)Activator.CreateInstance(mbBodyType);
                fresh?.Init(min, max);

                if (fresh != null)
                    Reflection.SetPropertyValue(Base, "BodyPropertyRange", fresh);

                return fresh;
            }

            // Try to use Clone to break sharing.
            try
            {
                if (
                    Reflection.InvokeMethod(current, "Clone", Type.EmptyTypes)
                    is MBBodyProperty clone
                )
                {
                    Reflection.SetPropertyValue(Base, "BodyPropertyRange", clone);
                    return clone;
                }
            }
            catch
            {
                // Ignore and fall through to manual clone.
            }

            var min1 = Base.GetBodyPropertiesMin();
            var max1 = Base.GetBodyPropertiesMax();

            var type = current.GetType();
            var fresh2 = (MBBodyProperty)Activator.CreateInstance(type);
            fresh2?.Init(min1, max1);

            if (fresh2 != null)
                Reflection.SetPropertyValue(Base, "BodyPropertyRange", fresh2);

            return fresh2;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Serialized Body Envelope                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public const string BodySerializedSeparator = "\n---BODY_MAX---\n";

        /// <summary>
        /// Full body envelope (BodyPropertyMin/Max) as a single serialized string.
        /// Uses BodyProperties.ToString/FromString to capture both dynamic and static
        /// face data. This is the only body attribute that needs persistence.
        /// </summary>
        MAttribute<string> BodySerializedAttribute =>
            Attribute(
                getter: _ => SerializeBodyEnvelope(),
                setter: (_, value) => ApplySerializedBodyEnvelope(value)
            );

        /// <summary>
        /// Serialize the current BodyPropertyMin/Max into a single string.
        /// </summary>
        string SerializeBodyEnvelope()
        {
            // Prefer the live MBBodyProperty if present; otherwise fall back to
            // CharacterObject's min/max helpers.
            var range = GetBodyRangeOrNull();

            BodyProperties min;
            BodyProperties max;

            if (range != null)
            {
                min = range.BodyPropertyMin;
                max = range.BodyPropertyMax;
            }
            else
            {
                min = Base.GetBodyPropertiesMin();
                max = Base.GetBodyPropertiesMax();
            }

            return min.ToString() + BodySerializedSeparator + max.ToString();
        }

        /// <summary>
        /// Apply a serialized envelope string back onto this character's
        /// BodyPropertyRange.
        /// </summary>
        void ApplySerializedBodyEnvelope(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            var parts = value.Split([BodySerializedSeparator], StringSplitOptions.None);
            if (parts.Length != 2)
            {
                Log.Warn($"WCharacter: invalid body envelope for '{Base?.StringId}': '{value}'");
                return;
            }

            if (!BodyProperties.FromString(parts[0], out var min))
                return;

            if (!BodyProperties.FromString(parts[1], out var max))
                return;

#if BL13
            // Capture tag pools from the CURRENT range (whatever persistence already applied).
            var current = GetBodyRangeOrNull();
            string hair = current?.HairTags ?? string.Empty;
            string beard = current?.BeardTags ?? string.Empty;
            string tattoo = current?.TattooTags ?? string.Empty;
#endif

            var range = EnsureOwnBodyRange();
            range?.Init(min, max);

#if BL13
            // Restore tag pools directly onto the range we just re-initialized.
            if (range != null)
            {
                range.HairTags = hair;
                range.BeardTags = beard;
                range.TattooTags = tattoo;
            }
#endif
        }
    }
}
