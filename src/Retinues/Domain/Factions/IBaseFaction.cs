using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.Core;
#if BL13 || BL14
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Domain.Factions
{
    public interface IBaseFaction
    {
        /* ━━━━━━━ Identity ━━━━━━━ */

        string StringId { get; }

        /* ━━━━━━━━━━ XML ━━━━━━━━━ */

        string Serialize();
        string SerializeAll();
        string Deserialize(string xml);

        /* ━━━━━━━━━ Main ━━━━━━━━━ */

        string Name { get; }
        uint Color { get; }
        uint Color2 { get; }
        Banner Banner { get; }
        WHero Leader { get; }

        /* ━━━━━━━━━ Image ━━━━━━━━ */

# if BL13 || BL14
        public BannerImageIdentifierVM Image { get; }
#else
        public ImageIdentifierVM Image { get; }
#endif

        public ImageIdentifier ImageIdentifier { get; }

        /* ━━━━━━━━━ Roots ━━━━━━━━ */

        WCharacter RootElite { get; }
        WCharacter RootBasic { get; }

        List<WCharacter> RosterElite { get; }
        List<WCharacter> RosterBasic { get; }

        /* ━━━━━━━━ Heroes ━━━━━━━━ */

        List<WCharacter> RosterHeroes { get; }

        /* ━━━━━━━ Villager ━━━━━━━ */

        WCharacter Villager { get; }

        /* ━━━━━━━━ Militia ━━━━━━━ */

        WCharacter MeleeMilitiaTroop { get; }
        WCharacter MeleeEliteMilitiaTroop { get; }
        WCharacter RangedMilitiaTroop { get; }
        WCharacter RangedEliteMilitiaTroop { get; }

        /* ━━━━━━━━ Caravan ━━━━━━━ */

        WCharacter CaravanGuard { get; }
        WCharacter CaravanMaster { get; }
        WCharacter ArmedTrader { get; }

        /* ━━━━━━━━━ Lists ━━━━━━━━ */

        List<WCharacter> RosterRetinues { get; }
        List<WCharacter> RosterMercenary { get; }
        List<WCharacter> RosterMilitia { get; }
        List<WCharacter> RosterCaravan { get; }
        List<WCharacter> RosterVillager { get; }
        List<WCharacter> RosterBandit { get; }
        List<WCharacter> RosterCivilian { get; }

        /* ━━━━━━ Convenience ━━━━━ */

        IEnumerable<WCharacter> Troops { get; }
    }
}
