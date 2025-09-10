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

        public MobileParty Base => _party;

        private WRoster _roster;

        public WRoster Roster
        {
            get
            {
                _roster ??= new WRoster(_party.MemberRoster);
                return _roster;
            }
        }

        public override string StringId => _party.StringId;

        // ================================================================
        // Misc
        // ================================================================

        public int PartySizeLimit => _party.Party.PartySizeLimit;
    }
}
