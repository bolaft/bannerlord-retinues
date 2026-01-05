using Retinues.Domain.Equipments.Helpers;
using Retinues.Framework.Model.Attributes;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Wrappers
{
    public partial class WCharacter
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void UpdateFormationClass()
        {
            if (FormationClassOverride != FormationClass.Unset)
                ApplyFormationInfo(FormationClassHelper.FromFormationClass(FormationClassOverride));
            else if (FirstBattleEquipment != null)
                ApplyFormationInfo(FirstBattleEquipment.FormationInfo);
        }

        private void ApplyFormationInfo(FormationClassHelper.FormationInfo info)
        {
            FormationClass = info.FormationClass;
            FormationGroup = info.FormationGroup;
            IsRanged = info.IsRanged;
            IsMounted = info.IsMounted;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Attributes                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<FormationClass> FormationClassOverrideAttribute =>
            Attribute(initialValue: FormationClass.Unset);

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

        public FormationClass FormationClass
        {
            get => FormationClassAttribute.Get();
            set => FormationClassAttribute.Set(value);
        }

        MAttribute<int> FormationGroupAttribute =>
            Attribute<int>(nameof(CharacterObject.DefaultFormationGroup));

        public int FormationGroup
        {
            get => FormationGroupAttribute.Get();
            set => FormationGroupAttribute.Set(value);
        }

        MAttribute<bool> IsRangedAttribute => Attribute<bool>(nameof(CharacterObject.IsRanged));

        public bool IsRanged
        {
            get => IsRangedAttribute.Get();
            set => IsRangedAttribute.Set(value);
        }

        MAttribute<bool> IsMountedAttribute => Attribute<bool>(nameof(CharacterObject.IsMounted));

        public bool IsMounted
        {
            get => IsMountedAttribute.Get();
            set => IsMountedAttribute.Set(value);
        }
    }
}
