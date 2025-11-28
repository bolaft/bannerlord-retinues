using MCM.Implementation;
using Retinues.Game.Wrappers;
using Retinues.Mods;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Localization;

namespace Retinues.GUI.Helpers
{
    [SafeClass]
    public static class Icons
    {
        public static string GetFormationClassIcon(WCharacter troop)
        {
            string icon = (troop?.FormationClass) switch
            {
                FormationClass.Infantry => @"General\TroopTypeIcons\icon_troop_type_infantry",
                FormationClass.Ranged => @"General\TroopTypeIcons\icon_troop_type_bow",
                FormationClass.Cavalry => @"General\TroopTypeIcons\icon_troop_type_cavalry",
                FormationClass.HorseArcher =>
                    @"General\TroopTypeIcons\icon_troop_type_horse_archer",
                _ => @"General\TroopTypeIcons\icon_troop_type_infantry",
            };

            if (ModCompatibility.HasNavalDLC)
                if (troop != null && troop.IsMariner)
                    if (
                        troop.FormationClass == FormationClass.Infantry
                        || troop.FormationClass == FormationClass.Ranged
                    )
                        icon = $"{icon}_mariner_big";

            return icon;
        }

        public static StringItemWithHintVM GetTierIconData(WCharacter troop)
        {
            if (troop == null)
                return new StringItemWithHintVM(string.Empty, new TextObject(string.Empty));

            return CampaignUIHelper.GetCharacterTierData(troop.Base, isBig: true);
        }
    }
}
