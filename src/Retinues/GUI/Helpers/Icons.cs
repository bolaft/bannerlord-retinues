using Retinues.Game.Wrappers;
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
            return (troop?.FormationClass) switch
            {
                FormationClass.Infantry => @"General\TroopTypeIcons\icon_troop_type_infantry",
                FormationClass.Ranged => @"General\TroopTypeIcons\icon_troop_type_bow",
                FormationClass.Cavalry => @"General\TroopTypeIcons\icon_troop_type_cavalry",
                FormationClass.HorseArcher =>
                    @"General\TroopTypeIcons\icon_troop_type_horse_archer",
                _ => @"General\TroopTypeIcons\icon_troop_type_infantry",
            };
        }

        public static StringItemWithHintVM GetTierIconData(WCharacter troop)
        {
            if (troop == null)
                return new StringItemWithHintVM(string.Empty, new TextObject(string.Empty));

            return CampaignUIHelper.GetCharacterTierData(troop.Base, isBig: true);
        }
    }
}
