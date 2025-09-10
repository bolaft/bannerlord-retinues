using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Game.Wrappers
{
    public class WParty(MobileParty party) : StringIdentifier
    {
        private readonly MobileParty _party = party;

        public MobileParty Base => _party;

        public override string StringId => _party.StringId;

        private WRoster _memberRoster;

        public WRoster MemberRoster
        {
            get
            {
                _memberRoster ??= new WRoster(_party.MemberRoster, this);
                return _memberRoster;
            }
        }

        public WRoster PrisonRoster
        {
            get
            {
                if (_party.PrisonRoster == null)
                    return null;
                return new WRoster(_party.PrisonRoster, this);
            }
        }

        // ================================================================
        // Misc
        // ================================================================

        public int PartySizeLimit => _party.Party.PartySizeLimit;
    }
}
