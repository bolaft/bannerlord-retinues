using System.Collections.Generic;
using Retinues.Model.Characters;
using TaleWorlds.Core;

namespace Retinues.Model.Factions
{
    public interface IBaseFaction
    {
        /* ━━━━━━━━━ Main ━━━━━━━━━ */

        string Name { get; }
        uint Color { get; }
        uint Color2 { get; }
        Banner Banner { get; }

        /* ━━━━━━━━━ Roots ━━━━━━━━ */

        WCharacter RootElite { get; }
        WCharacter RootBasic { get; }

        List<WCharacter> RosterElite { get; }
        List<WCharacter> RosterBasic { get; }

        /* ━━━━━━━━ Heroes ━━━━━━━━ */

        List<WHero> RosterHeroes { get; }

        /* ━━━━━━━━━ Lists ━━━━━━━━ */

        List<WCharacter> RosterRetinues { get; }
        List<WCharacter> RosterMilitia { get; }
        List<WCharacter> RosterCaravan { get; }
        List<WCharacter> RosterVillager { get; }
        List<WCharacter> RosterBandit { get; }
        List<WCharacter> RosterCivilian { get; }
    }
}
