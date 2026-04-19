using System;
using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Framework.Model.Attributes;
using Retinues.Interface.Services;

namespace Retinues.Domain.Characters.Wrappers
{
    /// <summary>
    /// Captain-related custom fields and utilities for cloned/stubbed captain variants.
    /// </summary>
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Captain                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<bool> IsCaptainAttribute =>
            Attribute(initialValue: false, name: "IsCaptainAttribute");

        /// <summary>
        /// True if this character is a captain variant.
        /// </summary>
        public bool IsCaptain
        {
            get => IsCaptainAttribute.Get();
            set => IsCaptainAttribute.Set(value);
        }

        MAttribute<bool> IsCaptainEnabledAttribute =>
            Attribute(initialValue: false, name: "IsCaptainEnabledAttribute");

        /// <summary>
        /// Whether the captain variant is enabled for recruitment/conversion.
        /// </summary>
        public bool IsCaptainEnabled
        {
            get => IsCaptainEnabledAttribute.Get();
            set => IsCaptainEnabledAttribute.Set(value);
        }

        MAttribute<string> CaptainIdAttribute =>
            Attribute<string>(initialValue: null, name: "CaptainIdAttribute");

        /// <summary>
        /// The captain variant of this unit, if one exists.
        /// </summary>
        public WCharacter Captain
        {
            get
            {
                var id = CaptainIdAttribute.Get();
                return string.IsNullOrEmpty(id) ? null : Get(id);
            }
            set => CaptainIdAttribute.Set(value?.StringId);
        }

        /// <summary>
        /// True if this unit has an associated captain variant.
        /// </summary>
        public bool HasCaptain => Captain != null;

        MAttribute<string> CaptainBaseIdAttribute =>
            Attribute<string>(initialValue: null, name: "CaptainBaseIdAttribute");

        /// <summary>
        /// For captain variants, the base unit they were cloned from.
        /// </summary>
        public WCharacter CaptainBase
        {
            get
            {
                var id = CaptainBaseIdAttribute.Get();
                return string.IsNullOrEmpty(id) ? null : Get(id);
            }
            set => CaptainBaseIdAttribute.Set(value?.StringId);
        }

        /// <summary>
        /// Creates and links a captain variant for this unit (shares skill-point pool if configured).
        /// </summary>
        public WCharacter CreateCaptain()
        {
            if (IsCaptain)
                return null;

            if (HasCaptain)
                return Captain;

            var captain = CharacterCloner.Clone(this, skills: true, equipments: true);
            if (captain == null)
                return null;

            captain.IsCaptain = true;
            captain.IsCaptainEnabled = false;
            captain.CaptainBase = this;

            // Non-max-tier base troops get a one-tier bump; max-tier base troops
            // stay at the same tier (their bonus comes from SkillRules instead).
            // Use absolute assignment to avoid residual level state from the clone.
            captain.Level = IsMaxTier ? Level : Level + 5;

            // Ensure captains are not shown visible.
            captain.HiddenInEncyclopedia = true;

            // Name: "<Base> <CaptainSuffix>"
            captain.Name = MakeCaptainName(Name);

            // Link back from base troop.
            Captain = captain;

            return captain;
        }

        /// <summary>
        /// Builds a display name for a captain variant from the base troop name.
        /// </summary>
        private static string MakeCaptainName(string baseName)
        {
            var suffix = L.S("captain_suffix", "Captain");
            if (string.IsNullOrEmpty(baseName))
                return suffix;

            // Avoid double suffixing
            if (baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return baseName;

            return $"{baseName} {suffix}";
        }
    }
}
