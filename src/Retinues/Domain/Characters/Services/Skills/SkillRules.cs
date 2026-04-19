using System;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Compatibility;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Settings;

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
                0 => Configuration.SkillCapT0,
                1 => Configuration.SkillCapT1,
                2 => Configuration.SkillCapT2,
                3 => Configuration.SkillCapT3,
                4 => Configuration.SkillCapT4,
                5 => Configuration.SkillCapT5,
                6 => Configuration.SkillCapT6,
                7 => Configuration.SkillCapT7,
                _ => Configuration.SkillCapT7,
            };

            /// <summary>
            /// Computes any bonus to be added to the base total.
            /// </summary>
            int ComputeBonus()
            {
                const float MaxTierCaptainMultiplier = 0.2f;

                int bonus = 0;

                if (wc.IsRetinue)
                    bonus += Configuration.RetinueSkillCapBonus;

                if (wc.IsCaptain && wc.IsElite && wc.IsMaxTier)
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
                0 => Configuration.SkillTotalT0,
                1 => Configuration.SkillTotalT1,
                2 => Configuration.SkillTotalT2,
                3 => Configuration.SkillTotalT3,
                4 => Configuration.SkillTotalT4,
                5 => Configuration.SkillTotalT5,
                6 => Configuration.SkillTotalT6,
                7 => Configuration.SkillTotalT7,
                _ => Configuration.SkillTotalT7,
            };

            /// <summary>
            /// Computes any applicable bonus to the skill total.
            /// </summary>
            int ComputeBonus()
            {
                const float MaxTierCaptainMultiplier = 0.2f;

                int bonus = 0;

                if (wc.IsRetinue)
                    bonus += Configuration.RetinueSkillTotalBonus;

                if (wc.IsCaptain && wc.IsElite && wc.IsMaxTier)
                    bonus = (int)(total * MaxTierCaptainMultiplier);

                if (DoctrineCatalog.SteadfastSoldiers.IsAcquired && wc.IsBasic)
                    bonus += 20;

                if (DoctrineCatalog.MastersAtArms.IsAcquired && wc.IsElite)
                    bonus += 20;

                return bonus;
            }

            return total + ComputeBonus();
        }
    }
}
