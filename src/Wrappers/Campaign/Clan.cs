using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Objects;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class WClan(Clan clan) : IWrapper
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

        public string StringId => _clan.StringId.ToString();

        // =========================================================================
        // Troops
        // =========================================================================

        public List<WCharacter> EliteTroops { get; } = [];
        public List<WCharacter> BasicTroops { get; } = [];
    }
}
