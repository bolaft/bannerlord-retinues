using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class WClan(Clan clan) : StringIdentifier, IWrapper
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly Clan _clan = clan;

        public object Base => _clan;

        // =========================================================================
        // Properties
        // =========================================================================

        public string Name => _clan.Name.ToString();

        public override string StringId => _clan.StringId.ToString();

        public string BannerCodeText => _clan.Banner.Serialize();

        public uint Color => _clan.Color;

        public uint Color2 => _clan.Color2;

        // =========================================================================
        // Troops
        // =========================================================================

        public List<WCharacter> EliteTroops { get; } = [];
        public List<WCharacter> BasicTroops { get; } = [];
    }
}
