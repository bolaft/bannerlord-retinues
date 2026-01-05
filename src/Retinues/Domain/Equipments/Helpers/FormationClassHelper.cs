using System;
using Retinues.Domain.Equipments.Models;
using TaleWorlds.Core;

namespace Retinues.Domain.Equipments.Helpers
{
    public static class FormationClassHelper
    {
        public readonly struct FormationInfo(
            FormationClass formationClass,
            bool isRanged,
            bool isMounted
        )
        {
            public readonly FormationClass FormationClass = formationClass;
            public readonly int FormationGroup = (int)formationClass;
            public readonly bool IsRanged = isRanged;
            public readonly bool IsMounted = isMounted;
        }

        public static FormationInfo Compute(MEquipment equipment)
        {
            if (equipment == null)
                throw new ArgumentNullException(nameof(equipment));

            // These slot indices are vanilla Bannerlord EquipmentIndex.
            var horse = equipment.Get(EquipmentIndex.Horse);
            var mounted = horse != null && horse.IsHorse;

            var ranged =
                (equipment.Get(EquipmentIndex.Weapon0)?.IsRangedWeapon ?? false)
                || (equipment.Get(EquipmentIndex.Weapon1)?.IsRangedWeapon ?? false)
                || (equipment.Get(EquipmentIndex.Weapon2)?.IsRangedWeapon ?? false)
                || (equipment.Get(EquipmentIndex.Weapon3)?.IsRangedWeapon ?? false);

            // Simple, robust mapping.
            // If you have extra FormationClass values in your build, keep this conservative.
            if (mounted && ranged)
                return FromFormationClass(FormationClass.HorseArcher);
            if (mounted)
                return FromFormationClass(FormationClass.Cavalry);
            if (ranged)
                return FromFormationClass(FormationClass.Ranged);

            return FromFormationClass(FormationClass.Infantry);
        }

        public static FormationInfo FromFormationClass(FormationClass formationClass)
        {
            switch (formationClass)
            {
                case FormationClass.HorseArcher:
                    return new FormationInfo(
                        FormationClass.HorseArcher,
                        isRanged: true,
                        isMounted: true
                    );

                case FormationClass.Cavalry:
                    return new FormationInfo(
                        FormationClass.Cavalry,
                        isRanged: false,
                        isMounted: true
                    );

                case FormationClass.Ranged:
                    return new FormationInfo(
                        FormationClass.Ranged,
                        isRanged: true,
                        isMounted: false
                    );

                case FormationClass.Infantry:
                default:
                    return new FormationInfo(
                        FormationClass.Infantry,
                        isRanged: false,
                        isMounted: false
                    );
            }
        }
    }
}
