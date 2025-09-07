using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using Retinues.Core.Wrappers.Objects;
using Retinues.Core.Utils;

namespace Retinues.Core.Wrappers.Campaign
{
    public class WFaction(IFaction faction) : StringIdentifier, IWrapper
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly IFaction _faction = faction;

        public object Base => _faction;

        // =========================================================================
        // Properties
        // =========================================================================

        public string Name => _faction.Name.ToString();

        public override string StringId => _faction.StringId;

        public string BannerCodeText => _faction.Banner.Serialize();

        public uint Color => _faction.Color;

        public uint Color2 => _faction.Color2;

        public WCulture Culture => new(_faction.Culture);

        // =========================================================================
        // Troops
        // =========================================================================

        public WCharacter RootElite { get; set; }
        public WCharacter RootBasic { get; set; }

        public List<WCharacter> EliteTroops { get; } = [];
        public List<WCharacter> BasicTroops { get; } = [];
    }
}
