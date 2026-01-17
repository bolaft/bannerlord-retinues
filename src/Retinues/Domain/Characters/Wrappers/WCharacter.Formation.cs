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
            Attribute(initialValue: FormationClass.Unset);

        /// <summary>
        /// Optional override for the formation class; setting it updates derived flags.
        /// </summary>
        public FormationClass FormationClassOverride
        {
            get => FormationClassOverrideAttribute.Get();
            set
            {
                FormationClassOverrideAttribute.Set(value);
                UpdateFormationClass();
            }
        }

        MAttribute<FormationClass> FormationClassAttribute =>
            Attribute<FormationClass>(nameof(CharacterObject.DefaultFormationClass));

        /// <summary>
        /// The character's effective formation class (Infantry/Ranged/Cavalry/etc.).
        /// </summary>
        public FormationClass FormationClass
        {
            get => FormationClassAttribute.Get();
            set => FormationClassAttribute.Set(value);
        }

        MAttribute<int> FormationGroupAttribute =>
            Attribute<int>(nameof(CharacterObject.DefaultFormationGroup));

        /// <summary>
        /// The formation group index used for formation placement.
        /// </summary>
        public int FormationGroup
        {
            get => FormationGroupAttribute.Get();
            set => FormationGroupAttribute.Set(value);
        }

        MAttribute<bool> IsRangedAttribute => Attribute<bool>(nameof(CharacterObject.IsRanged));

        /// <summary>
        /// True if the character is considered ranged for queries and behavior.
        /// </summary>
        public bool IsRanged
        {
            get => IsRangedAttribute.Get();
            set => IsRangedAttribute.Set(value);
        }

        MAttribute<bool> IsMountedAttribute => Attribute<bool>(nameof(CharacterObject.IsMounted));

        /// <summary>
        /// True if the character is considered mounted for queries and behavior.
        /// </summary>
        public bool IsMounted
        {
            get => IsMountedAttribute.Get();
            set => IsMountedAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Updates formation class and related flags from overrides or equipment.
        /// </summary>
        public void UpdateFormationClass()
        {
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
            FormationClass = info.FormationClass;
            FormationGroup = info.FormationGroup;
            IsRanged = info.IsRanged;
            IsMounted = info.IsMounted;
        }
    }
}
