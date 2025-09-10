using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Core.Game.Wrappers
{
    public class WRosterElement(TroopRosterElement element, WRoster roster, int index)
    {
        private readonly int _index = index;
        private readonly TroopRosterElement _element = element;
        private readonly WRoster _roster = roster;
        private readonly WCharacter _troop = new(element.Character);

        public int Index => _index;
        public TroopRosterElement Base => _element;
        public WRoster Roster => _roster;
        public WCharacter Troop => _troop;

        public int Number => _element.Number;
        public int WoundedNumber => _element.WoundedNumber;
        public int Xp => _element.Xp;
    }
}
