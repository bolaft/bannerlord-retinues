using System.Collections.Concurrent;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Domain.Equipments.Helpers
{
    /// <summary>
    /// Computes comparative "chevrons" between items and caches results for reuse.
    /// </summary>
    public static class ItemComparisonHelper
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Cache                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Cached chevron pair (positive/negative) for a comparison key.
        /// </summary>
        private struct ChevronCacheEntry
        {
            public int Positive;
            public int Negative;
        }

        private static readonly ConcurrentDictionary<string, ChevronCacheEntry> _chevronCache = [];

        /// <summary>
        /// Computes positive/negative chevrons comparing a against b, using cache when possible.
        /// </summary>
        public static void GetComparisonChevrons(
            WItem a,
            WItem b,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            positiveChevrons = 0;
            negativeChevrons = 0;

            if (a == null || b == null)
                return;

            if (ReferenceEquals(a, b))
                return;

            var key = GetChevronCacheKey(a, b);

            if (_chevronCache.TryGetValue(key, out var cached))
            {
                positiveChevrons = cached.Positive;
                negativeChevrons = cached.Negative;
                return;
            }

            ComputeComparisonChevronScore(a, b, out positiveChevrons, out negativeChevrons);

            _chevronCache[key] = new ChevronCacheEntry
            {
                Positive = positiveChevrons,
                Negative = negativeChevrons,
            };
        }

        private static string GetChevronCacheKey(WItem a, WItem b)
        {
            var idA = a?.StringId ?? "__NULL_A__";
            var idB = b?.StringId ?? "__NULL_B__";
            return idA + "=>" + idB;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Comparison                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Performs the domain-specific comparison and produces chevron counts.
        /// </summary>
        private static void ComputeComparisonChevronScore(
            WItem a,
            WItem b,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            positiveChevrons = 0;
            negativeChevrons = 0;

            // Weapons (melee / ranged / shields / ammo)
            if (a.IsWeapon && b.IsWeapon)
            {
                var w1 = a.PrimaryWeapon;
                var w2 = b.PrimaryWeapon;
                if (w1 == null || w2 == null)
                    return;

                if (a.Type != b.Type)
                    return;

                if (a.IsShield || b.IsShield)
                {
                    if (!a.IsShield || !b.IsShield)
                        return;

                    CompareShieldChevrons(a, b, out positiveChevrons, out negativeChevrons);
                    return;
                }

                if (a.IsAmmo || b.IsAmmo)
                {
                    if (!a.IsAmmo || !b.IsAmmo)
                        return;

                    CompareAmmoChevrons(a, b, out positiveChevrons, out negativeChevrons);
                    return;
                }

                if (a.IsRangedWeapon && b.IsRangedWeapon)
                {
                    CompareRangedWeaponChevrons(a, b, out positiveChevrons, out negativeChevrons);
                    return;
                }

                if (a.IsMeleeWeapon && b.IsMeleeWeapon)
                {
                    CompareMeleeWeaponChevrons(a, b, out positiveChevrons, out negativeChevrons);
                    return;
                }

                return;
            }

            // Human armor (head, body, hands, legs)
            if (
                a.ArmorComponent != null
                && b.ArmorComponent != null
                && a.Type != ItemObject.ItemTypeEnum.HorseHarness
                && b.Type != ItemObject.ItemTypeEnum.HorseHarness
            )
            {
                if (a.Type != b.Type)
                    return;

                CompareArmorChevrons(a, b, out positiveChevrons, out negativeChevrons);
                return;
            }

            // Horse harness
            if (
                a.Type == ItemObject.ItemTypeEnum.HorseHarness
                && b.Type == ItemObject.ItemTypeEnum.HorseHarness
                && a.ArmorComponent != null
                && b.ArmorComponent != null
            )
            {
                CompareHorseHarnessChevrons(a, b, out positiveChevrons, out negativeChevrons);
                return;
            }

            // Horses
            if (a.IsHorse && b.IsHorse && a.HorseComponent != null && b.HorseComponent != null)
            {
                CompareHorseChevrons(a, b, out positiveChevrons, out negativeChevrons);
                return;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Aggregation                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Helper to accumulate a single stat comparison into better/worse counters.
        /// </summary>
        private static void AccumulateStatComparison(
            int thisValue,
            int otherValue,
            bool higherIsBetter,
            ref int better,
            ref int worse
        )
        {
            if (thisValue == otherValue)
                return;

            bool thisBetter = higherIsBetter ? thisValue > otherValue : thisValue < otherValue;

            if (thisBetter)
                better++;
            else
                worse++;
        }

        /// <summary>
        /// Converts counts of better/worse stats into positive/negative chevrons.
        /// </summary>
        private static void GetChevronsFromCounts(
            int better,
            int worse,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            positiveChevrons = 0;
            negativeChevrons = 0;

            int nonEqual = better + worse;
            if (nonEqual == 0)
                return;

            if (better == worse)
                return;

            if (worse == 0)
            {
                positiveChevrons = 3;
                negativeChevrons = 0;
                return;
            }

            if (better == 0)
            {
                positiveChevrons = 0;
                negativeChevrons = 3;
                return;
            }

            if (better > worse)
            {
                positiveChevrons = 2;
                negativeChevrons = 1;
            }
            else
            {
                positiveChevrons = 1;
                negativeChevrons = 2;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Comparers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Compare melee weapon stats and derive chevrons.
        /// </summary>
        private static void CompareMeleeWeaponChevrons(
            WItem a,
            WItem b,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var w1 = a.PrimaryWeapon;
            var w2 = b.PrimaryWeapon;

            int better = 0,
                worse = 0;

            AccumulateStatComparison(w1.SwingDamage, w2.SwingDamage, true, ref better, ref worse);
            AccumulateStatComparison(w1.ThrustDamage, w2.ThrustDamage, true, ref better, ref worse);

            AccumulateStatComparison(w1.SwingSpeed, w2.SwingSpeed, true, ref better, ref worse);
            AccumulateStatComparison(w1.ThrustSpeed, w2.ThrustSpeed, true, ref better, ref worse);

            AccumulateStatComparison(w1.WeaponLength, w2.WeaponLength, true, ref better, ref worse);
            AccumulateStatComparison(w1.Handling, w2.Handling, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        /// <summary>
        /// Compare ranged weapon stats and derive chevrons.
        /// </summary>
        private static void CompareRangedWeaponChevrons(
            WItem a,
            WItem b,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var w1 = a.PrimaryWeapon;
            var w2 = b.PrimaryWeapon;

            int better = 0,
                worse = 0;

            AccumulateStatComparison(w1.SwingDamage, w2.SwingDamage, true, ref better, ref worse);
            AccumulateStatComparison(w1.ThrustDamage, w2.ThrustDamage, true, ref better, ref worse);

            AccumulateStatComparison(w1.MissileSpeed, w2.MissileSpeed, true, ref better, ref worse);
            AccumulateStatComparison(w1.Accuracy, w2.Accuracy, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        /// <summary>
        /// Compare ammo stats and derive chevrons.
        /// </summary>
        private static void CompareAmmoChevrons(
            WItem a,
            WItem b,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var w1 = a.PrimaryWeapon;
            var w2 = b.PrimaryWeapon;

            int better = 0,
                worse = 0;

            AccumulateStatComparison(w1.SwingDamage, w2.SwingDamage, true, ref better, ref worse);
            AccumulateStatComparison(w1.ThrustDamage, w2.ThrustDamage, true, ref better, ref worse);

            AccumulateStatComparison(w1.MissileSpeed, w2.MissileSpeed, true, ref better, ref worse);
            AccumulateStatComparison(w1.Accuracy, w2.Accuracy, true, ref better, ref worse);

            int thisStack = w1.MaxDataValue;
            int otherStack = w2.MaxDataValue;
            AccumulateStatComparison(thisStack, otherStack, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        /// <summary>
        /// Compare shield stats and derive chevrons.
        /// </summary>
        private static void CompareShieldChevrons(
            WItem a,
            WItem b,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var w1 = a.PrimaryWeapon;
            var w2 = b.PrimaryWeapon;

            int better = 0,
                worse = 0;

            int thisHp = w1.MaxDataValue;
            int otherHp = w2.MaxDataValue;
            AccumulateStatComparison(thisHp, otherHp, true, ref better, ref worse);

            AccumulateStatComparison(w1.BodyArmor, w2.BodyArmor, true, ref better, ref worse);
            AccumulateStatComparison(w1.Handling, w2.Handling, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        /// <summary>
        /// Compare armor stats and derive chevrons.
        /// </summary>
        private static void CompareArmorChevrons(
            WItem a,
            WItem b,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var a1 = a.ArmorComponent;
            var a2 = b.ArmorComponent;

            if (a1 == null || a2 == null)
            {
                positiveChevrons = 0;
                negativeChevrons = 0;
                return;
            }

            int better = 0,
                worse = 0;

            AccumulateStatComparison(a1.HeadArmor, a2.HeadArmor, true, ref better, ref worse);
            AccumulateStatComparison(a1.BodyArmor, a2.BodyArmor, true, ref better, ref worse);
            AccumulateStatComparison(a1.ArmArmor, a2.ArmArmor, true, ref better, ref worse);
            AccumulateStatComparison(a1.LegArmor, a2.LegArmor, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        /// <summary>
        /// Compare horse harness stats and derive chevrons.
        /// </summary>
        private static void CompareHorseHarnessChevrons(
            WItem a,
            WItem b,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var a1 = a.ArmorComponent;
            var a2 = b.ArmorComponent;

            if (a1 == null || a2 == null)
            {
                positiveChevrons = 0;
                negativeChevrons = 0;
                return;
            }

            int better = 0,
                worse = 0;

            AccumulateStatComparison(a1.BodyArmor, a2.BodyArmor, true, ref better, ref worse);
            AccumulateStatComparison(
                a1.ManeuverBonus,
                a2.ManeuverBonus,
                true,
                ref better,
                ref worse
            );
            AccumulateStatComparison(a1.SpeedBonus, a2.SpeedBonus, true, ref better, ref worse);
            AccumulateStatComparison(a1.ChargeBonus, a2.ChargeBonus, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }

        /// <summary>
        /// Compare horse stats and derive chevrons.
        /// </summary>
        private static void CompareHorseChevrons(
            WItem a,
            WItem b,
            out int positiveChevrons,
            out int negativeChevrons
        )
        {
            var h1 = a.HorseComponent;
            var h2 = b.HorseComponent;

            if (h1 == null || h2 == null)
            {
                positiveChevrons = 0;
                negativeChevrons = 0;
                return;
            }

            int better = 0,
                worse = 0;

            AccumulateStatComparison(h1.Speed, h2.Speed, true, ref better, ref worse);
            AccumulateStatComparison(h1.Maneuver, h2.Maneuver, true, ref better, ref worse);
            AccumulateStatComparison(h1.ChargeDamage, h2.ChargeDamage, true, ref better, ref worse);

            int thisHp = h1.HitPoints + h1.HitPointBonus;
            int otherHp = h2.HitPoints + h2.HitPointBonus;
            AccumulateStatComparison(thisHp, otherHp, true, ref better, ref worse);

            GetChevronsFromCounts(better, worse, out positiveChevrons, out negativeChevrons);
        }
    }
}
