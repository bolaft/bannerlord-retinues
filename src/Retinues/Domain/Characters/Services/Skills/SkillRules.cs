using System;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Compatibility;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;

namespace Retinues.Domain.Characters.Services.Skills
{
    public static class SkillRules
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Consts                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public const int MaxSkillLevel = 360;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Caps                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Clamps the given tier to valid range.
        /// </summary>
        static int ClampTier(int tier) =>
            Math.Max(0, Math.Min(Mods.T7TroopUnlocker.IsLoaded ? 7 : 6, tier));

        /// <summary>
        /// Gets the skill cap for the given wrapped character.
        /// </summary>
        public static int GetSkillCap(WCharacter wc)
        {
            if (wc == null)
                return 0;

            if (wc.IsHero)
                return MaxSkillLevel;

            var tier = ClampTier(wc.Tier);
            var cap = tier switch
            {
                0 => Settings.SkillCapT0,
                1 => Settings.SkillCapT1,
                2 => Settings.SkillCapT2,
                3 => Settings.SkillCapT3,
                4 => Settings.SkillCapT4,
                5 => Settings.SkillCapT5,
                6 => Settings.SkillCapT6,
                7 => Settings.SkillCapT7,
                _ => Settings.SkillCapT7,
            };

            /// <summary>
            /// Computes any bonus to be added to the base total.
            /// </summary>
            int ComputeBonus()
            {
                const float MaxTierCaptainMultiplier = 0.2f;

                int bonus = 0;

                if (wc.IsRetinue)
                    bonus += Settings.RetinueSkillCapBonus;

                if (wc.IsCaptain && wc.IsMaxTier)
                    bonus = (int)(cap * MaxTierCaptainMultiplier);

                if (DoctrineCatalog.IronDiscipline.IsAcquired)
                    bonus += 5;

                return bonus;
            }

            return cap + ComputeBonus();
        }

        /// <summary>
        /// Gets the skill total for the given wrapped character.
        /// </summary>
        public static int GetSkillTotal(WCharacter wc)
        {
            if (wc == null)
                return 0;

            if (wc.IsHero)
                return int.MaxValue;

            var tier = ClampTier(wc.Tier);
            var total = tier switch
            {
                0 => Settings.SkillTotalT0,
                1 => Settings.SkillTotalT1,
                2 => Settings.SkillTotalT2,
                3 => Settings.SkillTotalT3,
                4 => Settings.SkillTotalT4,
                5 => Settings.SkillTotalT5,
                6 => Settings.SkillTotalT6,
                7 => Settings.SkillTotalT7,
                _ => Settings.SkillTotalT7,
            };

            /// <summary>
            /// Computes any applicable bonus to the skill total.
            /// </summary>
            int ComputeBonus()
            {
                const float MaxTierCaptainMultiplier = 0.2f;

                int bonus = 0;

                if (wc.IsRetinue)
                    bonus += Settings.RetinueSkillTotalBonus;

                if (wc.IsCaptain && wc.IsMaxTier)
                    bonus = (int)(total * MaxTierCaptainMultiplier);

                if (DoctrineCatalog.SteadfastSoldiers.IsAcquired && wc.IsBasic)
                    bonus += 20;

                if (DoctrineCatalog.MastersAtArms.IsAcquired && wc.IsElite)
                    bonus += 20;

                return bonus;
            }

            Log.Info($"Base skill total for tier {tier} is {total}.");
            var bonus = ComputeBonus();
            Log.Info($"Computed bonus is {bonus}.");

            return total + bonus;
        }
    }
}
