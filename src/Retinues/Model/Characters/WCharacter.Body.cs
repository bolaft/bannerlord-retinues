using System;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Model.Characters
{
    public partial class WCharacter : WBase<WCharacter, CharacterObject>
    {
        bool _bodyInitialized;

        void EnsureBodyInitialized()
        {
            if (_bodyInitialized)
                return;

            _bodyInitialized = true;

            // 1) Force-create Culture attribute FIRST, so Base.Culture is restored
            // before any body setters run. We assume WCharacter has a Culture property.
            object _ = Culture;

            // 2) Force-create all body attributes, in a deterministic order.
            _ = BodyAgeMinAttribute;
            _ = BodyAgeMaxAttribute;
            _ = BodyWeightMinAttribute;
            _ = BodyWeightMaxAttribute;
            _ = BodyBuildMinAttribute;
            _ = BodyBuildMaxAttribute;
            _ = HairTagsAttribute;
            _ = BeardTagsAttribute;
            _ = TattooTagsAttribute;
        }

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
        //                        Age Range                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<float> _bodyAgeMinAttribute;
        MAttribute<float> BodyAgeMinAttribute =>
            _bodyAgeMinAttribute ??= new MAttribute<float>(
                baseInstance: Base,
                getter: _ => Base.GetBodyPropertiesMin().Age,
                setter: (_, value) =>
                    SetBodyDynamicEnd(minEnd: true, age: value, weight: null, build: null),
                targetName: "body_age_min",
                persistent: true
            );

        public float AgeMin
        {
            get => BodyAgeMinAttribute.Get();
            set => BodyAgeMinAttribute.Set(value);
        }

        MAttribute<float> _bodyAgeMaxAttribute;
        MAttribute<float> BodyAgeMaxAttribute =>
            _bodyAgeMaxAttribute ??= new MAttribute<float>(
                baseInstance: Base,
                getter: _ => Base.GetBodyPropertiesMax().Age,
                setter: (_, value) =>
                    SetBodyDynamicEnd(minEnd: false, age: value, weight: null, build: null),
                targetName: "body_age_max",
                persistent: true
            );

        public float AgeMax
        {
            get => BodyAgeMaxAttribute.Get();
            set => BodyAgeMaxAttribute.Set(value);
        }

        MAttribute<float> _bodyWeightMinAttribute;
        MAttribute<float> BodyWeightMinAttribute =>
            _bodyWeightMinAttribute ??= new MAttribute<float>(
                baseInstance: Base,
                getter: _ => Base.GetBodyPropertiesMin().Weight,
                setter: (_, value) =>
                    SetBodyDynamicEnd(minEnd: true, age: null, weight: value, build: null),
                targetName: "body_weight_min",
                persistent: true
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Weight Range                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public float WeightMin
        {
            get => BodyWeightMinAttribute.Get();
            set => BodyWeightMinAttribute.Set(value);
        }

        MAttribute<float> _bodyWeightMaxAttribute;
        MAttribute<float> BodyWeightMaxAttribute =>
            _bodyWeightMaxAttribute ??= new MAttribute<float>(
                baseInstance: Base,
                getter: _ => Base.GetBodyPropertiesMax().Weight,
                setter: (_, value) =>
                    SetBodyDynamicEnd(minEnd: false, age: null, weight: value, build: null),
                targetName: "body_weight_max",
                persistent: true
            );

        public float WeightMax
        {
            get => BodyWeightMaxAttribute.Get();
            set => BodyWeightMaxAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Build Range                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<float> _bodyBuildMinAttribute;
        MAttribute<float> BodyBuildMinAttribute =>
            _bodyBuildMinAttribute ??= new MAttribute<float>(
                baseInstance: Base,
                getter: _ => Base.GetBodyPropertiesMin().Build,
                setter: (_, value) =>
                    SetBodyDynamicEnd(minEnd: true, age: null, weight: null, build: value),
                targetName: "body_build_min",
                persistent: true
            );

        public float BuildMin
        {
            get => BodyBuildMinAttribute.Get();
            set => BodyBuildMinAttribute.Set(value);
        }

        MAttribute<float> _bodyBuildMaxAttribute;
        MAttribute<float> BodyBuildMaxAttribute =>
            _bodyBuildMaxAttribute ??= new MAttribute<float>(
                baseInstance: Base,
                getter: _ => Base.GetBodyPropertiesMax().Build,
                setter: (_, value) =>
                    SetBodyDynamicEnd(minEnd: false, age: null, weight: null, build: value),
                targetName: "body_build_max",
                persistent: true
            );

        public float BuildMax
        {
            get => BodyBuildMaxAttribute.Get();
            set => BodyBuildMaxAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Tags                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ Hair Tags ━━━━━━ */

        MAttribute<string> _hairTagsAttribute;
        MAttribute<string> HairTagsAttribute =>
            _hairTagsAttribute ??= new MAttribute<string>(
                baseInstance: Base,
                getter: _ =>
                {
                    var range = GetBodyRangeOrNull();
                    return range?.HairTags ?? string.Empty;
                },
                setter: (_, value) =>
                {
                    var range = EnsureOwnBodyRange();
                    if (range != null)
                        range.HairTags = value ?? string.Empty;
                },
                targetName: "body_hair_tags",
                persistent: true
            );

        public string HairTags
        {
            get => HairTagsAttribute.Get();
            set => HairTagsAttribute.Set(value);
        }

        /* ━━━━━━ Beard Tags ━━━━━━ */

        MAttribute<string> _beardTagsAttribute;
        MAttribute<string> BeardTagsAttribute =>
            _beardTagsAttribute ??= new MAttribute<string>(
                baseInstance: Base,
                getter: _ =>
                {
                    var range = GetBodyRangeOrNull();
                    return range?.BeardTags ?? string.Empty;
                },
                setter: (_, value) =>
                {
                    var range = EnsureOwnBodyRange();
                    if (range != null)
                        range.BeardTags = value ?? string.Empty;
                },
                targetName: "body_beard_tags",
                persistent: true
            );

        public string BeardTags
        {
            get => BeardTagsAttribute.Get();
            set => BeardTagsAttribute.Set(value);
        }

        /* ━━━━━━ Tattoo Tags ━━━━━ */

        MAttribute<string> _tattooTagsAttribute;
        MAttribute<string> TattooTagsAttribute =>
            _tattooTagsAttribute ??= new MAttribute<string>(
                baseInstance: Base,
                getter: _ =>
                {
                    var range = GetBodyRangeOrNull();
                    return range?.TattooTags ?? string.Empty;
                },
                setter: (_, value) =>
                {
                    var range = EnsureOwnBodyRange();
                    if (range != null)
                        range.TattooTags = value ?? string.Empty;
                },
                targetName: "body_tattoo_tags",
                persistent: true
            );

        public string TattooTags
        {
            get => TattooTagsAttribute.Get();
            set => TattooTagsAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        void SetBodyDynamicEnd(bool minEnd, float? age, float? weight, float? build)
        {
            // Skip heroes for now; we will handle them via Hero.BodyProperties later.
            if (Base.IsHero)
                return;

            var range = EnsureOwnBodyRange();
            if (range == null)
                return;

            var min = range.BodyPropertyMin;
            var max = range.BodyPropertyMax;

            var src = minEnd ? min : max;
            var oth = minEnd ? max : min;

            // Current dynamic values for the end we are editing.
            var dyn = src.DynamicProperties;

            var newDyn = new DynamicBodyProperties(
                age ?? dyn.Age,
                weight ?? dyn.Weight,
                build ?? dyn.Build
            );

            // ───── Choose static from current culture template if available ─────

            StaticBodyProperties srcStatic;
            StaticBodyProperties othStatic;

            var culture = Base.Culture;
            CharacterObject template = culture?.BasicTroop ?? culture?.EliteBasicTroop;

            if (template != null)
            {
                var tplMin = template.GetBodyPropertiesMin();
                var tplMax = template.GetBodyPropertiesMax();

                // Use template static for BOTH ends of the range so appearance matches culture.
                var tplMinStatic = tplMin.StaticProperties;
                var tplMaxStatic = tplMax.StaticProperties;

                srcStatic = minEnd ? tplMinStatic : tplMaxStatic;
                othStatic = minEnd ? tplMaxStatic : tplMinStatic;
            }
            else
            {
                // Fallback: keep existing static if culture has no template.
                srcStatic = src.StaticProperties;
                othStatic = oth.StaticProperties;
            }

            var newSrc = new BodyProperties(newDyn, srcStatic);
            var newOth = new BodyProperties(oth.DynamicProperties, othStatic);

            var newMin = minEnd ? newSrc : newOth;
            var newMax = minEnd ? newOth : newSrc;

            range.Init(newMin, newMax);
        }

        MBBodyProperty GetBodyRangeOrNull() =>
            Reflection.GetPropertyValue<MBBodyProperty>(Base, "BodyPropertyRange");

        MBBodyProperty EnsureOwnBodyRange()
        {
            if (Base.IsHero)
                return null;

            try
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
                    var clone =
                        Reflection.InvokeMethod(current, "Clone", Type.EmptyTypes)
                        as MBBodyProperty;
                    if (clone != null)
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
            catch (Exception ex)
            {
                Log.Exception(ex);
                return null;
            }
        }
    }
}
