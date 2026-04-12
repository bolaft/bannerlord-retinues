using System;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Helpers
{
    public static class BodyHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Applies body properties from the culture's default template for the given race.
        /// </summary>
        public static bool ApplyCultureBodyPropertiesForRace(WCharacter wc, int race)
        {
            if (wc == null)
                return false;

            var template = FindTemplateForRace(wc, race);

            if (template == null)
            {
                Log.Warning(
                    $"No suitable template for culture '{wc.Culture?.StringId}', gender={wc.IsFemale}, race={race}, aborting."
                );
                return false;
            }

            // Break shared reference
            EnsureOwnBodyRange(wc);

            var range = Reflection.GetPropertyValue<object>(wc.Base, "BodyPropertyRange");
            if (range == null)
            {
                Log.Warning("Missing BodyPropertyRange on target, aborting.");
                return false;
            }

            // 1) Apply chosen race (do NOT let template override it)
            wc.Race = race;

            // 2) Copy min/max envelope from template
            var minTroop = template.Base.GetBodyPropertiesMin();
            var maxTroop = template.Base.GetBodyPropertiesMax();

            Reflection.InvokeMethod(
                range,
                "Init",
                [typeof(BodyProperties), typeof(BodyProperties)],
                minTroop,
                maxTroop
            );

            // 3) Snap age to the template's mid-age
            wc.Age = (minTroop.Age + maxTroop.Age) * 0.5f;

            // 4) Snap hair/scar/tattoo tags from that template
            ApplyTagsFromCulture(wc, template);

            return true;
        }

        /// <summary>
        /// Finds a template troop for the given culture, gender, and race.
        /// </summary>
        public static WCharacter FindTemplateForRace(WCharacter wc, int race)
        {
            if (wc == null)
                return null;

            // Prefer the same sources as ApplyCultureBodyProperties, but filtered by race.
            var culture = wc.Culture;
            if (culture == null)
                return null;

            // 1) Roots
            var root = culture.RootBasic ?? culture.RootElite;
            if (root != null && root.IsFemale == wc.IsFemale && root.Race == race)
                return root;

            // 2) Villagers
            var villager = wc.IsFemale ? culture.VillageWoman : culture.Villager;
            if (villager != null && villager.IsFemale == wc.IsFemale && villager.Race == race)
                return villager;

            // 3) Any troop in roster matching gender+race
            foreach (var troop in culture.Troops)
            {
                if (troop == null)
                    continue;

                if (troop.IsFemale == wc.IsFemale && troop.Race == race)
                    return troop;
            }

            return null;
        }

        /// <summary>
        /// Applies body properties from the culture's default template.
        /// </summary>
        public static void ApplyCultureBodyProperties(WCharacter wc)
        {
            if (wc == null)
                return;

            var culture = wc.Culture;
            if (culture == null)
                return;

            var template = culture.RootBasic ?? culture.RootElite;

            if (template?.IsFemale != wc.IsFemale)
                template = wc.IsFemale ? culture.VillageWoman : culture.Villager;

            if (template == null)
            {
                foreach (var troop in culture.Troops)
                {
                    template = troop;
                    if (troop.IsFemale == wc.IsFemale)
                        break;
                }
            }

            if (template == null)
            {
                Log.Warning($"No suitable template for culture '{culture.StringId}', aborting.");
                return;
            }

            // Break shared reference
            EnsureOwnBodyRange(wc);

            var range = Reflection.GetPropertyValue<object>(wc.Base, "BodyPropertyRange");

            // 1) Copy race.
            wc.Race = template.Race;

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
            wc.Age = (minTroop.Age + maxTroop.Age) * 0.5f;

            // 5) Re-snap hair/scar/tattoo tags from culture template
            ApplyTagsFromCulture(wc, template);
        }

        /// <summary>
        /// Applies hair/beard/tattoo tags from the given culture template.
        /// </summary>
        public static void ApplyTagsFromCulture(WCharacter wc, WCharacter template)
        {
            if (wc == null || template == null)
                return;

#if BL13 || BL14
            var templateRange = template.Base.BodyPropertyRange;
            var range = wc.Base.BodyPropertyRange;

            if (templateRange == null || range == null)
            {
                Log.Warning("Missing BodyPropertyRange on template or target, aborting.");
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
                Log.Debug("Template has no tags, nothing to apply.");
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
                SetHairTags(wc, hairTags);

            if (hasBeard)
                SetBeardTags(wc, beardTags);

            if (hasTattoo)
                SetTattooTags(wc, tattooTags);
#endif
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Height Range                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Reads the height value (0..1) from the given WCharacter's body properties.
        /// </summary>
        public static float ReadHeight(WCharacter wc, bool minEnd)
        {
            if (wc == null)
                return 0f;

            var bp = minEnd ? wc.Base.GetBodyPropertiesMin() : wc.Base.GetBodyPropertiesMax();
            var sp = bp.StaticProperties;

            ulong part = GetKeyPart(sp, WCharacter.HEIGHT_PART);
            int raw = GetBitsValueFromKey(part, WCharacter.HEIGHT_START, WCharacter.HEIGHT_BITS);

            int max = (1 << WCharacter.HEIGHT_BITS) - 1;
            return max > 0 ? raw / (float)max : 0f;
        }

        /// <summary>
        /// Sets the height value (0..1) on the given WCharacter's body properties.
        /// </summary>
        public static void SetHeightEnd(WCharacter wc, bool minEnd, float value01) =>
            SetStaticChannelEnd(
                wc,
                minEnd,
                WCharacter.HEIGHT_PART,
                WCharacter.HEIGHT_START,
                WCharacter.HEIGHT_BITS,
                value01
            );

        /// <summary>
        /// Sets a static body property channel end (min or max) for the given WCharacter.
        /// </summary>
        public static void SetStaticChannelEnd(
            WCharacter wc,
            bool minEnd,
            int partIdx,
            int startBit,
            int numBits,
            float value01
        )
        {
            if (wc == null)
                return;

            wc.BodySerializedAttribute.Touch(); // mark dirty

            var range = EnsureOwnBodyRange(wc);
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

        /// <summary>
        /// Sets dynamic body property ends (min or max) for the given WCharacter.
        /// </summary>
        public static void SetBodyDynamicEnd(
            WCharacter wc,
            bool minEnd,
            float? age,
            float? weight,
            float? build
        )
        {
            if (wc == null)
                return;

            wc.BodySerializedAttribute.Touch(); // mark dirty

            var range = EnsureOwnBodyRange(wc);
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

        /// <summary>
        /// Gets the BodyPropertyRange from the given WCharacter, or null if missing.
        /// </summary>
        public static MBBodyProperty GetBodyRangeOrNull(WCharacter wc) =>
            wc == null
                ? null
                : Reflection.GetPropertyValue<MBBodyProperty>(wc.Base, "BodyPropertyRange");

        /// <summary>
        /// Ensures the given WCharacter has its own BodyPropertyRange instance.
        /// </summary>
        public static MBBodyProperty EnsureOwnBodyRange(WCharacter wc)
        {
            if (wc == null)
                return null;

            var current = GetBodyRangeOrNull(wc);
            if (current == null)
            {
                var min = wc.Base.GetBodyPropertiesMin();
                var max = wc.Base.GetBodyPropertiesMax();

                var mbBodyType = typeof(BodyProperties).Assembly.GetType(
                    "TaleWorlds.Core.MBBodyProperty"
                );

                if (mbBodyType == null)
                    return null;

                var fresh = (MBBodyProperty)Activator.CreateInstance(mbBodyType);
                fresh?.Init(min, max);

                if (fresh != null)
                    Reflection.SetPropertyValue(wc.Base, "BodyPropertyRange", fresh);

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
                    Reflection.SetPropertyValue(wc.Base, "BodyPropertyRange", clone);
                    return clone;
                }
            }
            catch
            {
                // Ignore and fall through to manual clone.
            }

            var min1 = wc.Base.GetBodyPropertiesMin();
            var max1 = wc.Base.GetBodyPropertiesMax();

            var type = current.GetType();
            var fresh2 = (MBBodyProperty)Activator.CreateInstance(type);
            fresh2?.Init(min1, max1);

            if (fresh2 != null)
                Reflection.SetPropertyValue(wc.Base, "BodyPropertyRange", fresh2);

            return fresh2;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Tag Setters                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Sets hair tags on the given WCharacter.
        /// </summary>
        public static void SetHairTags(WCharacter wc, string value)
        {
#if BL13 || BL14
            if (wc == null)
                return;

            var clonedRange = MBBodyProperty.CreateFrom(wc.Base.BodyPropertyRange);
            clonedRange.HairTags = value ?? string.Empty;
            Reflection.SetPropertyValue(wc.Base, "BodyPropertyRange", clonedRange);
#endif
        }

        /// <summary>
        /// Sets beard tags on the given WCharacter.
        /// </summary>
        public static void SetBeardTags(WCharacter wc, string value)
        {
#if BL13 || BL14
            if (wc == null)
                return;

            var clonedRange = MBBodyProperty.CreateFrom(wc.Base.BodyPropertyRange);
            clonedRange.BeardTags = value ?? string.Empty;
            Reflection.SetPropertyValue(wc.Base, "BodyPropertyRange", clonedRange);
#endif
        }

        /// <summary>
        /// Sets tattoo tags on the given WCharacter.
        /// </summary>
        public static void SetTattooTags(WCharacter wc, string value)
        {
#if BL13 || BL14
            if (wc == null)
                return;

            var clonedRange = MBBodyProperty.CreateFrom(wc.Base.BodyPropertyRange);
            clonedRange.TattooTags = value ?? string.Empty;
            Reflection.SetPropertyValue(wc.Base, "BodyPropertyRange", clonedRange);
#endif
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Serialized Body Envelope                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Serializes the body envelope (min/max) of the given WCharacter to a string.
        /// </summary>
        public static string SerializeBodyEnvelope(WCharacter wc)
        {
            if (wc == null)
                return string.Empty;

            // Prefer the live MBBodyProperty if present; otherwise fall back to
            // CharacterObject's min/max helpers.
            var range = GetBodyRangeOrNull(wc);

            BodyProperties min;
            BodyProperties max;

            if (range != null)
            {
                min = range.BodyPropertyMin;
                max = range.BodyPropertyMax;
            }
            else
            {
                min = wc.Base.GetBodyPropertiesMin();
                max = wc.Base.GetBodyPropertiesMax();
            }

            return min.ToString() + WCharacter.BodySerializedSeparator + max.ToString();
        }

        /// <summary>
        /// Applies a serialized body envelope (min/max) string to the given WCharacter.
        /// </summary>
        public static void ApplySerializedBodyEnvelope(WCharacter wc, string value)
        {
            if (wc == null)
                return;

            if (string.IsNullOrEmpty(value))
                return;

            var parts = value.Split([WCharacter.BodySerializedSeparator], StringSplitOptions.None);

            if (parts.Length != 2)
            {
                Log.Warning(
                    $"WCharacter: invalid body envelope for '{wc.Base?.StringId}': '{value}'"
                );
                return;
            }

            if (!BodyProperties.FromString(parts[0], out var min))
                return;

            if (!BodyProperties.FromString(parts[1], out var max))
                return;

#if BL13 || BL14
            // Capture tag pools from the CURRENT range (whatever persistence already applied).
            var current = GetBodyRangeOrNull(wc);
            string hair = current?.HairTags ?? string.Empty;
            string beard = current?.BeardTags ?? string.Empty;
            string tattoo = current?.TattooTags ?? string.Empty;
#endif

            var range = EnsureOwnBodyRange(wc);
            range?.Init(min, max);

#if BL13 || BL14
            // Restore tag pools directly onto the range we just re-initialized.
            if (range != null)
            {
                range.HairTags = hair;
                range.BeardTags = beard;
                range.TattooTags = tattoo;
            }
#endif
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Static KeyPart helpers                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clamps a float value to the [0..1] range.
        /// </summary>
        public static float Clamp01(float v)
        {
            if (v < 0f)
                return 0f;
            if (v > 1f)
                return 1f;
            return v;
        }

        /// <summary>
        /// Gets a bitfield value from a ulong key part.
        /// </summary>
        public static int GetBitsValueFromKey(ulong part, int startBit, int numBits)
        {
            ulong shifted = part >> startBit;
            ulong mask = (1UL << numBits) - 1UL;
            return (int)(shifted & mask);
        }

        /// <summary>
        /// Sets a bitfield value into a ulong key part.
        /// </summary>
        public static ulong SetBits(ulong part, int startBit, int numBits, int newValue)
        {
            ulong mask = ((1UL << numBits) - 1UL) << startBit;
            return (part & ~mask) | ((ulong)newValue << startBit);
        }

        /// <summary>
        /// Gets a key part from StaticBodyProperties by index (1..8).
        /// </summary>
        public static ulong GetKeyPart(in StaticBodyProperties sp, int idx) =>
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

        /// <summary>
        /// Sets a key part into StaticBodyProperties by index (1..8).
        /// </summary>
        public static StaticBodyProperties SetKeyPart(
            in StaticBodyProperties sp,
            int idx,
            ulong val
        ) =>
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
    }
}
