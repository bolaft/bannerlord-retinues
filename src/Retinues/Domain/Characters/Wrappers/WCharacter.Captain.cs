using System;
using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Framework.Model.Attributes;
using Retinues.GUI.Services;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Captain                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<bool> IsCaptainAttribute => Attribute(initialValue: false);

        public bool IsCaptain
        {
            get => IsCaptainAttribute.Get();
            set => IsCaptainAttribute.Set(value);
        }

        MAttribute<bool> IsCaptainEnabledAttribute => Attribute(initialValue: false);

        public bool IsCaptainEnabled
        {
            get => IsCaptainEnabledAttribute.Get();
            set => IsCaptainEnabledAttribute.Set(value);
        }

        MAttribute<string> CaptainIdAttribute => Attribute<string>(initialValue: null);

        /// <summary>
        /// The captain variant of this unit, if any.
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

        public bool HasCaptain => Captain != null;

        MAttribute<string> CaptainBaseIdAttribute => Attribute<string>(initialValue: null);

        /// <summary>
        /// For captains only, the base unit they were created from.
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

        MAttribute<string> SkillPointsOwnerIdAttribute => Attribute<string>(initialValue: null);

        /// <summary>
        /// Creates a new captain variant for this unit if none exists.
        /// Captains are cloned stubs and are independent from the base troop
        /// except for skill points which can be shared with the base troop.
        /// </summary>
        public WCharacter CreateCaptain(bool skills = true, bool equipments = true)
        {
            if (IsCaptain)
                return null;

            if (HasCaptain)
                return Captain;

            var captain = CharacterCloner.Clone(this, skills: skills, equipments: equipments);
            if (captain == null)
                return null;

            captain.IsCaptain = true;
            captain.IsCaptainEnabled = false;

            captain.CaptainBase = this;

            // Share the skill point pool with the base troop.
            captain.SkillPointsOwnerIdAttribute.Set(StringId);

            // Name: "<Base> <CaptainSuffix>"
            captain.Name = MakeCaptainName(Name);

            // Link back from base troop.
            Captain = captain;

            return captain;
        }

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
