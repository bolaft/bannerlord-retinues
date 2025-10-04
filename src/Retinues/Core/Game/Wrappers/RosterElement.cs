using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Core.Game.Wrappers
{
    [SafeClass(SwallowByDefault = false)]
    public class WRosterElement(TroopRosterElement element, WRoster roster, int index)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly int _index = index;
        private readonly TroopRosterElement _element = element;
        private readonly WRoster _roster = roster;
        private readonly WCharacter _troop = new(element.Character);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        public TroopRosterElement Base => _element;
        public WRoster Roster => _roster;
        public WCharacter Troop => _troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Attributes                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int Index => _index;
        public int Number => _roster.Base.GetElementNumber(_index);
        public int WoundedNumber => _roster.Base.GetElementWoundedNumber(_index);
        public int Xp => _roster.Base.GetElementXp(_index);
    }
}
