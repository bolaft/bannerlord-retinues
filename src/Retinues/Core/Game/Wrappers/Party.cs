using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Wrappers
{
    public class WParty(MobileParty party) : StringIdentifier
    {
        private readonly MobileParty _party = party;

        public override string StringId => _party.StringId;

        // ================================================================
        // Misc
        // ================================================================

        public int PartySizeLimit => _party.Party.PartySizeLimit;

        // ================================================================
        // Troop access
        // ================================================================

        public IEnumerable<WCharacter> Troops => _party.MemberRoster.GetTroopRoster().Select(elem => new WCharacter(elem.Character));

        public int TroopCount => _party.MemberRoster.TotalManCount;

        public int CountOf(WCharacter troop)
        {
            return troop == null ? 0 : _party.MemberRoster.GetTroopCount(troop.Base as CharacterObject);
        }

        // ================================================================
        // Modification
        // ================================================================

        public void AddTroop(WCharacter troop, int count = 1, bool wounded = false)
        {
            _party.MemberRoster.AddToCounts(
                troop.Base as CharacterObject,
                count,
                insertAtFront: false,
                woundedCount: wounded ? count : 0);
        }

        public void RemoveTroop(WCharacter troop, int count = 1)
        {
            if (troop.Base == null) return;

            _party.MemberRoster.AddToCounts(troop.Base as CharacterObject, -count);
        }
    }
}
