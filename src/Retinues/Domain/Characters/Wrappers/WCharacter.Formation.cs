using Retinues.Domain.Equipments.Helpers;
using Retinues.Framework.Model.Attributes;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Wrappers
{
    /// <summary>
    /// Formation-related attributes and helpers for the wrapped character.
    /// </summary>
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Attributes                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<FormationClass> FormationClassOverrideAttribute =>
            Attribute(initialValue: FormationClass.Unset, name: "FormationClassOverrideAttribute");

        /// <summary>
        /// Optional override for the formation class; setting it updates derived flags.
        /// </summary>
        public FormationClass FormationClassOverride
        {
            get => NonVariantBase().FormationClassOverrideAttribute.Get();
            set
            {
                NonVariantBase().FormationClassOverrideAttribute.Set(value);
                NonVariantBase().UpdateFormationClass();
            }
        }

        MAttribute<FormationClass> FormationClassAttribute =>
            Attribute<FormationClass>(
                nameof(CharacterObject.DefaultFormationClass),
                name: "FormationClassAttribute"
            );

        /// <summary>
        /// The character's effective formation class (Infantry/Ranged/Cavalry/etc.).
        /// </summary>
        public FormationClass FormationClass
        {
            get => NonVariantBase().FormationClassAttribute.Get();
            set => NonVariantBase().FormationClassAttribute.Set(value);
        }

        MAttribute<int> FormationGroupAttribute =>
            Attribute<int>(
                nameof(CharacterObject.DefaultFormationGroup),
                name: "FormationGroupAttribute"
            );

        /// <summary>
        /// The formation group index used for formation placement.
        /// </summary>
        public int FormationGroup
        {
            get => NonVariantBase().FormationGroupAttribute.Get();
            set => NonVariantBase().FormationGroupAttribute.Set(value);
        }

        MAttribute<bool> IsRangedAttribute =>
            Attribute<bool>(nameof(CharacterObject.IsRanged), name: "IsRangedAttribute");

        /// <summary>
        /// True if the character is considered ranged for queries and behavior.
        /// </summary>
        public bool IsRanged
        {
            get => NonVariantBase().IsRangedAttribute.Get();
            set => NonVariantBase().IsRangedAttribute.Set(value);
        }

        MAttribute<bool> IsMountedAttribute =>
            Attribute<bool>(nameof(CharacterObject.IsMounted), name: "IsMountedAttribute");

        /// <summary>
        /// True if the character is considered mounted for queries and behavior.
        /// </summary>
        public bool IsMounted
        {
            get => NonVariantBase().IsMountedAttribute.Get();
            set => NonVariantBase().IsMountedAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Updates formation class and related flags from overrides or equipment.
        /// </summary>
        public void UpdateFormationClass()
        {
            if (IsVariant)
                return; // only apply on base variant

            if (FormationClassOverride != FormationClass.Unset)
                ApplyFormationInfo(FormationClassHelper.FromFormationClass(FormationClassOverride));
            else if (FirstBattleEquipment != null)
                ApplyFormationInfo(FirstBattleEquipment.FormationInfo);
        }

        /// <summary>
        /// Applies formation info (class/group/mounted/ranged) to this character.
        /// </summary>
        private void ApplyFormationInfo(FormationClassHelper.FormationInfo info)
        {
            if (IsVariant)
                return; // only apply on base variant

            FormationClass = info.FormationClass;
            FormationGroup = info.FormationGroup;
            IsRanged = info.IsRanged;
            IsMounted = info.IsMounted;
        }
    }
}
