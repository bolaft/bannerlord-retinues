using TaleWorlds.CampaignSystem;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class HeroWrapper
    {
        public ClanWrapper Clan => new ClanWrapper(Hero.MainHero.Clan);

        public CultureWrapper Culture => new CultureWrapper(Hero.MainHero.Culture);
    }
}
