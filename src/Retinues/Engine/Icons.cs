using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Engine
{
    [SafeClass]
    public static class Icons
    {
        const string BaseIconPath = @"General\TroopTypeIcons\";
        const string MarinerSuffix = "_mariner_big";
        const string IconInfantry = "icon_troop_type_infantry";
        const string IconRanged = "icon_troop_type_bow";
        const string IconCavalry = "icon_troop_type_cavalry";
        const string IconHorseArcher = "icon_troop_type_horse_archer";

        public static string GetFormationClassIcon(WCharacter troop, bool mariner = false)
        {
            string icon = (troop?.FormationClass) switch
            {
                FormationClass.Infantry => BaseIconPath + IconInfantry,
                FormationClass.HeavyInfantry => BaseIconPath + IconInfantry,
                FormationClass.Bodyguard => BaseIconPath + IconInfantry,
                FormationClass.Ranged => BaseIconPath + IconRanged,
                FormationClass.Skirmisher => BaseIconPath + IconRanged,
                FormationClass.Cavalry => BaseIconPath + IconCavalry,
                FormationClass.LightCavalry => BaseIconPath + IconCavalry,
                FormationClass.HeavyCavalry => BaseIconPath + IconCavalry,
                FormationClass.General => BaseIconPath + IconCavalry,
                FormationClass.HorseArcher => BaseIconPath + IconHorseArcher,
                _ => BaseIconPath + IconInfantry,
            };

            if (mariner)
            {
                // Apply mariner variant for any icon that is the infantry or ranged base icon.
                if (icon == BaseIconPath + IconInfantry || icon == BaseIconPath + IconRanged)
                    icon += MarinerSuffix;
            }

            return icon;
        }
    }
}
