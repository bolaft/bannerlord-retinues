using TaleWorlds.CampaignSystem;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class ClanWrapper(Clan clan)
    {
        // =========================================================================
        // Base
        // =========================================================================

        private Clan _clan = clan;

        public Clan Base => _clan;

        // =========================================================================
        // Properties
        // =========================================================================

        public string Name => _clan.Name.ToString();

        public string StringId => _clan.StringId.ToString();
    }
}
