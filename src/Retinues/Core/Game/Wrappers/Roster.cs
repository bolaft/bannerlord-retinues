using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Core.Game.Wrappers
{
    public class WRoster(TroopRoster roster)
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly TroopRoster _roster = roster;

        public object Base => _roster;

        // ================================================================
        // Troops
        // ================================================================

        public IEnumerable<WCharacter> Troops => _roster.GetTroopRoster().Select(elem => new WCharacter(elem.Character));

        public int TroopCount => _roster.TotalManCount;

        public int CountOf(WCharacter troop)
        {
            return troop == null ? 0 : _roster.GetTroopCount(troop.Base as CharacterObject);
        }

        public void AddTroop(WCharacter troop, int count = 1, bool wounded = false)
        {
            _roster.AddToCounts(
                troop.Base as CharacterObject,
                count,
                insertAtFront: false,
                woundedCount: wounded ? count : 0);
        }

        public void RemoveTroop(WCharacter troop, int count = 1)
        {
            if (troop.Base == null) return;

            _roster.AddToCounts(troop.Base as CharacterObject, -count);
        }
    }
}
