using System;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for all body/appearance properties of a WCharacter.
    /// For troops, edits CharacterObject ranges (BodyPropertyRange).
    /// For heroes, edits Hero body directly (BodyProperties / StaticBodyProperties).
    /// </summary>
    [SafeClass]
    public class WBody
    {
        private readonly WCharacter _owner;
        private CharacterObject Base => _owner.Base;
        private Hero Hero => Base?.HeroObject;
        private bool IsHero => Hero != null;

        public WBody(WCharacter owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                            Age                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public float Age
        {
            get => IsHero ? Hero.Age : Base.Age;
            set
            {
                if (IsHero)
                {
                    // Hero age is derived from birthday
                    Hero.SetBirthDay(CampaignTime.YearsFromNow(-value));
                }
                else
                {
                    Reflector.SetPropertyValue(Base, "Age", value);
                    _owner.NeedsPersistence = true;
                }
            }
        }

        public float AgeMin
        {
            get => IsHero ? Hero.Age : Base.GetBodyPropertiesMin().Age;
            set
            {
                if (IsHero)
                {
                    Hero.SetBirthDay(CampaignTime.YearsFromNow(-value));
                }
                else
                {
                    SetDynamicEnd(true, age: value, weight: null, build: null);
                }
            }
        }

        public float AgeMax
        {
            get => IsHero ? Hero.Age : Base.GetBodyPropertiesMax().Age;
            set
            {
                if (IsHero)
                {
                    Hero.SetBirthDay(CampaignTime.YearsFromNow(-value));
                }
                else
                {
                    SetDynamicEnd(false, age: value, weight: null, build: null);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Weight                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public float WeightMin
        {
            get => IsHero ? Hero.Weight : Base.GetBodyPropertiesMin().Weight;
            set
            {
                if (IsHero)
                    SetHeroDynamic(age: null, weight: value, build: null);
                else
                    SetDynamicEnd(true, age: null, weight: value, build: null);
            }
        }

        public float WeightMax
        {
            get => IsHero ? Hero.Weight : Base.GetBodyPropertiesMax().Weight;
            set
            {
                if (IsHero)
                    SetHeroDynamic(age: null, weight: value, build: null);
                else
                    SetDynamicEnd(false, age: null, weight: value, build: null);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Build                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public float BuildMin
        {
            get => IsHero ? Hero.Build : Base.GetBodyPropertiesMin().Build;
            set
            {
                if (IsHero)
                    SetHeroDynamic(age: null, weight: null, build: value);
                else
                    SetDynamicEnd(true, age: null, weight: null, build: value);
            }
        }

        public float BuildMax
        {
            get => IsHero ? Hero.Build : Base.GetBodyPropertiesMax().Build;
            set
            {
                if (IsHero)
                    SetHeroDynamic(age: null, weight: null, build: value);
                else
                    SetDynamicEnd(false, age: null, weight: null, build: value);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Dynamic Min/Max Editing              //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void SetDynamicEnd(bool minEnd, float? age, float? weight, float? build)
        {
            try
            {
                // For heroes, we ignore min/max ranges and just edit Hero.BodyProperties
                if (IsHero)
                {
                    SetHeroDynamic(age, weight, build);
                    return;
                }

                EnsureOwnBodyRange();

                var curMin = Base.GetBodyPropertiesMin();
                var curMax = Base.GetBodyPropertiesMax();

                var src = minEnd ? curMin : curMax;
                var oth = minEnd ? curMax : curMin;

                var dyn = src.DynamicProperties;
                var newDyn = new DynamicBodyProperties(
                    age ?? dyn.Age,
                    weight ?? dyn.Weight,
                    build ?? dyn.Build
                );

                var newSrc = new BodyProperties(newDyn, src.StaticProperties);

                var newMin = minEnd ? newSrc : oth;
                var newMax = minEnd ? oth : newSrc;

                var range = Reflector.GetPropertyValue<object>(Base, "BodyPropertyRange");
                Reflector.InvokeMethod(
                    range,
                    "Init",
                    new[] { typeof(BodyProperties), typeof(BodyProperties) },
                    newMin,
                    newMax
                );

                _owner.NeedsPersistence = true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private void SetHeroDynamic(float? age, float? weight, float? build)
        {
            if (!IsHero)
                return;

            try
            {
                var bp = Hero.BodyProperties;
                var dyn = bp.DynamicProperties;

                var newDyn = new DynamicBodyProperties(
                    age ?? dyn.Age,
                    weight ?? dyn.Weight,
                    build ?? dyn.Build
                );

                var newBp = new BodyProperties(newDyn, bp.StaticProperties);
#if BL13
                Hero.StaticBodyProperties = newBp.StaticProperties;
#else
                Reflector.SetPropertyValue(Hero, "StaticBodyProperties", newBp.StaticProperties);
#endif
                Hero.Weight = newDyn.Weight;
                Hero.Build = newDyn.Build;
                // Age is handled via SetBirthDay in Age/AgeMin/AgeMax
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Body Range                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void EnsureOwnBodyRange()
        {
            if (IsHero)
                return; // heroes don't use BodyPropertyRange for their live body

            try
            {
                var current = Reflector.GetPropertyValue<object>(Base, "BodyPropertyRange");
                if (current == null)
                {
                    var min = Base.GetBodyPropertiesMin();
                    var max = Base.GetBodyPropertiesMax();

                    var fresh = Activator.CreateInstance(
                        typeof(BodyProperties).Assembly.GetType("TaleWorlds.Core.MBBodyProperty")
                    );

                    if (fresh != null)
                    {
                        Reflector.InvokeMethod(
                            fresh,
                            "Init",
                            new[] { typeof(BodyProperties), typeof(BodyProperties) },
                            min,
                            max
                        );
                        Reflector.SetPropertyValue(Base, "BodyPropertyRange", fresh);
                    }
                    return;
                }

                try
                {
                    var clone = Reflector.InvokeMethod(current, "Clone", Type.EmptyTypes) as object;
                    if (clone != null)
                    {
                        Reflector.SetPropertyValue(Base, "BodyPropertyRange", clone);
                        return;
                    }
                }
                catch
                { /* ignore */
                }

                var min1 = Base.GetBodyPropertiesMin();
                var max1 = Base.GetBodyPropertiesMax();

                var type = current.GetType();
                var fresh2 = Activator.CreateInstance(type);
                Reflector.InvokeMethod(
                    fresh2,
                    "Init",
                    new[] { typeof(BodyProperties), typeof(BodyProperties) },
                    min1,
                    max1
                );
                Reflector.SetPropertyValue(Base, "BodyPropertyRange", fresh2);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Height                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const int HEIGHT_PART = 8;
        private const int HEIGHT_START = 19;
        private const int HEIGHT_BITS = 6;

        public float HeightMin
        {
            get => ReadHeight(true);
            set => SetHeight(true, value);
        }

        public float HeightMax
        {
            get => ReadHeight(false);
            set => SetHeight(false, value);
        }

        private float ReadHeight(bool minEnd)
        {
            if (IsHero)
            {
#if BL13
                var sp = Hero.StaticBodyProperties;
#else
                var sp = Reflector.GetPropertyValue<StaticBodyProperties>(
                    Hero,
                    "StaticBodyProperties"
                );
#endif
                ulong part = GetKeyPart(sp, HEIGHT_PART);
                int raw = GetBitsValueFromKey(part, HEIGHT_START, HEIGHT_BITS);
                int max = (1 << HEIGHT_BITS) - 1;
                return max > 0 ? raw / (float)max : 0f;
            }
            else
            {
                var bp = minEnd ? Base.GetBodyPropertiesMin() : Base.GetBodyPropertiesMax();
#if BL13
                var sp = Hero.StaticBodyProperties;
#else
                var sp = Reflector.GetPropertyValue<StaticBodyProperties>(
                    Hero,
                    "StaticBodyProperties"
                );
#endif
                ulong part = GetKeyPart(sp, HEIGHT_PART);
                int raw = GetBitsValueFromKey(part, HEIGHT_START, HEIGHT_BITS);
                int max = (1 << HEIGHT_BITS) - 1;
                return max > 0 ? raw / (float)max : 0f;
            }
        }

        private void SetHeight(bool minEnd, float value01)
        {
            try
            {
                if (IsHero)
                {
                    float v = Math.Max(0f, Math.Min(1f, value01));
                    int raw = (int)Math.Round(v * ((1 << HEIGHT_BITS) - 1));

#if BL13
                    var sp = Hero.StaticBodyProperties;
#else
                    var sp = Reflector.GetPropertyValue<StaticBodyProperties>(
                        Hero,
                        "StaticBodyProperties"
                    );
#endif
                    ulong part = GetKeyPart(sp, HEIGHT_PART);
                    part = SetBits(part, HEIGHT_START, HEIGHT_BITS, raw);
                    var newSp = SetKeyPart(sp, HEIGHT_PART, part);
#if BL13
                    Hero.StaticBodyProperties = newSp;
#else
                    Reflector.SetPropertyValue(Hero, "StaticBodyProperties", newSp);
#endif
                }
                else
                {
                    SetStaticChannelEnd(minEnd, HEIGHT_PART, HEIGHT_START, HEIGHT_BITS, value01);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private void SetStaticChannelEnd(
            bool minEnd,
            int partIdx,
            int startBit,
            int numBits,
            float value01
        )
        {
            try
            {
                EnsureOwnBodyRange();

                float v = Math.Max(0f, Math.Min(1f, value01));
                int raw = (int)Math.Round(v * ((1 << numBits) - 1));

                var curMin = Base.GetBodyPropertiesMin();
                var curMax = Base.GetBodyPropertiesMax();

                var src = minEnd ? curMin : curMax;
                var oth = minEnd ? curMax : curMin;

                var sp = src.StaticProperties;
                ulong part = GetKeyPart(sp, partIdx);

                part = SetBits(part, startBit, numBits, raw);

                var newSp = SetKeyPart(sp, partIdx, part);

                var newSrc = new BodyProperties(src.DynamicProperties, newSp);
                var newMin = minEnd ? newSrc : oth;
                var newMax = minEnd ? oth : newSrc;

                var range = Reflector.GetPropertyValue<object>(Base, "BodyPropertyRange");
                Reflector.InvokeMethod(
                    range,
                    "Init",
                    new[] { typeof(BodyProperties), typeof(BodyProperties) },
                    newMin,
                    newMax
                );

                _owner.NeedsPersistence = true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Static KeyPart helpers               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static int GetBitsValueFromKey(ulong part, int startBit, int numBits)
        {
            ulong shifted = part >> startBit;
            ulong mask = ((1UL << numBits) - 1);
            return (int)(shifted & mask);
        }

        private static ulong SetBits(ulong part, int startBit, int numBits, int newValue)
        {
            ulong mask = (((1UL << numBits) - 1) << startBit);
            return (part & ~mask) | ((ulong)newValue << startBit);
        }

        private static ulong GetKeyPart(in StaticBodyProperties sp, int idx) =>
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

        private static StaticBodyProperties SetKeyPart(
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
