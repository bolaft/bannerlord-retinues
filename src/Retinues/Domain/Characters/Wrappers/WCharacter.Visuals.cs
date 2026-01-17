using Retinues.Domain.Characters.Helpers;
using Retinues.Framework.Model.Attributes;
using TaleWorlds.CampaignSystem;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
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

        /// <summary>
        /// Applies body properties from the culture's default template for the given race.
        /// </summary>
        public bool ApplyCultureBodyPropertiesForRace(int race) =>
            BodyHelper.ApplyCultureBodyPropertiesForRace(this, race);

        /// <summary>
        /// Applies body properties from the culture's default template.
        /// </summary>
        public void ApplyCultureBodyProperties() => BodyHelper.ApplyCultureBodyProperties(this);

        /// <summary>
        /// Applies hair/beard/tattoo tags from the given culture template.
        /// </summary>
        public void ApplyTagsFromCulture(WCharacter template) =>
            BodyHelper.ApplyTagsFromCulture(this, template);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Height Range                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Height is NOT a DynamicBodyProperties channel (unlike age/weight/build).
        // It is encoded in StaticBodyProperties key parts (bitfield).

        internal const int HEIGHT_PART = 8;
        internal const int HEIGHT_START = 19;
        internal const int HEIGHT_BITS = 6;

        /// <summary>
        /// Sets the height range [0..1] for this character.
        /// </summary>
        public void SetHeightRange(float minHeight, float maxHeight)
        {
            HeightMin = minHeight;
            HeightMax = maxHeight;
        }

        public float HeightMin
        {
            get => BodyHelper.ReadHeight(this, minEnd: true);
            set => BodyHelper.SetHeightEnd(this, minEnd: true, value01: value);
        }

        public float HeightMax
        {
            get => BodyHelper.ReadHeight(this, minEnd: false);
            set => BodyHelper.SetHeightEnd(this, minEnd: false, value01: value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Age Range                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public float AgeMin
        {
            get => Base.GetBodyPropertiesMin().Age;
            set =>
                BodyHelper.SetBodyDynamicEnd(
                    this,
                    minEnd: true,
                    age: value,
                    weight: null,
                    build: null
                );
        }

        public float AgeMax
        {
            get => Base.GetBodyPropertiesMax().Age;
            set =>
                BodyHelper.SetBodyDynamicEnd(
                    this,
                    minEnd: false,
                    age: value,
                    weight: null,
                    build: null
                );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Weight Range                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public float WeightMin
        {
            get => Base.GetBodyPropertiesMin().Weight;
            set =>
                BodyHelper.SetBodyDynamicEnd(
                    this,
                    minEnd: true,
                    age: null,
                    weight: value,
                    build: null
                );
        }

        public float WeightMax
        {
            get => Base.GetBodyPropertiesMax().Weight;
            set =>
                BodyHelper.SetBodyDynamicEnd(
                    this,
                    minEnd: false,
                    age: null,
                    weight: value,
                    build: null
                );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Build Range                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public float BuildMin
        {
            get => Base.GetBodyPropertiesMin().Build;
            set =>
                BodyHelper.SetBodyDynamicEnd(
                    this,
                    minEnd: true,
                    age: null,
                    weight: null,
                    build: value
                );
        }

        public float BuildMax
        {
            get => Base.GetBodyPropertiesMax().Build;
            set =>
                BodyHelper.SetBodyDynamicEnd(
                    this,
                    minEnd: false,
                    age: null,
                    weight: null,
                    build: value
                );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Tags                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13
        /* ━━━━━━━ Hair Tags ━━━━━━ */

        MAttribute<string> HairTagsAttribute =>
            Attribute(
                getter: _ => Base.BodyPropertyRange.HairTags,
                setter: (_, value) => BodyHelper.SetHairTags(this, value)
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
                setter: (_, value) => BodyHelper.SetBeardTags(this, value)
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
                setter: (_, value) => BodyHelper.SetTattooTags(this, value)
            );

        public string TattooTags
        {
            get => TattooTagsAttribute.Get();
            set => TattooTagsAttribute.Set(value);
        }
#else
        // BL12: no tag pools on MBBodyProperty (keep API surface but no-op)
        public string HairTags
        {
            get => string.Empty;
            set { }
        }

        public string BeardTags
        {
            get => string.Empty;
            set { }
        }

        public string TattooTags
        {
            get => string.Empty;
            set { }
        }
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Serialized Body Envelope                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public const string BodySerializedSeparator = "|<MIN-MAX>|";

        /// <summary>
        /// Full body envelope (BodyPropertyMin/Max) as a single serialized string.
        /// Uses BodyProperties.ToString/FromString to capture both dynamic and static
        /// face data. This is the only body attribute that needs persistence.
        /// </summary>
        internal MAttribute<string> BodySerializedAttribute =>
            Attribute(
                getter: _ => BodyHelper.SerializeBodyEnvelope(this),
                setter: (_, value) => BodyHelper.ApplySerializedBodyEnvelope(this, value),
                priority: AttributePriority.Low,
                dependsOn:
                [
                    nameof(CultureAttribute),
                    nameof(IsFemaleAttribute),
                    nameof(RaceAttribute),
                    nameof(AgeAttribute),
                ]
            );

        /// <summary>
        /// Serialize the current BodyPropertyMin/Max into a single string.
        /// </summary>
        public string SerializeBodyEnvelope() => BodyHelper.SerializeBodyEnvelope(this);

        /// <summary>
        /// Apply a serialized envelope string back onto this character's BodyPropertyRange.
        /// </summary>
        public void ApplySerializedBodyEnvelope(string value) =>
            BodyHelper.ApplySerializedBodyEnvelope(this, value);
    }
}
