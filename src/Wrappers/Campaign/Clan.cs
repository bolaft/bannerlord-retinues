using TaleWorlds.CampaignSystem;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class ClanWrapper
    {
        public string Name { get; set; }

        public string StringId { get; set; }

        private Clan _clan;

        public Clan BaseClan => _clan;

        public ClanWrapper(Clan clan)
        {
            _clan = clan;
            Name = _clan.Name.ToString();
            StringId = _clan.StringId.ToString();
        }
    }
}
