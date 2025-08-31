using TaleWorlds.CampaignSystem;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class ClanWrapper
    {
        public string Name => _clan.Name.ToString();

        public string StringId => _clan.StringId.ToString();

        private Clan _clan;

        public ClanWrapper(Clan clan)
        {
            _clan = clan;
        }
    }
}
