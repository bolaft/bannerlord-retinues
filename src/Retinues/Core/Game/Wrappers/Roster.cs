using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Core.Game.Wrappers
{
    public class WRoster(TroopRoster roster, WParty party)
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly TroopRoster _roster = roster;

        public TroopRoster Base => _roster;

        private readonly WParty _party = party;

        public WParty Party => _party;

        // ================================================================
        // Troops
        // ================================================================

        public IEnumerable<WRosterElement> Elements
        {
            get
            {
                int i = 0;
                foreach (var element in _roster.GetTroopRoster())
                {
                    yield return new WRosterElement(element, this, i);
                    i++;
                }
            }
        }

        public int Count => _roster.Count;

        public int CountOf(WCharacter troop)
        {
            if (troop.Base == null) return 0;
            return _roster.GetTroopCount(troop.Base as CharacterObject);
        }

        public void AddTroop(WCharacter troop, int healthy, int wounded = 0, int index = -1)
        {
            _roster.AddToCounts(troop.Base as CharacterObject, healthy, woundedCount: wounded, index: index);
        }

        public void RemoveTroop(WCharacter troop, int healthy, int wounded = 0)
        {
            if (troop.Base == null) return;

            _roster.AddToCounts(troop.Base as CharacterObject, -healthy, woundedCount: -wounded);
        }
    }
}
