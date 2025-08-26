using TaleWorlds.CampaignSystem;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class HeroWrapper
    {
        public ClanWrapper Clan { get; set; }
        public CultureWrapper Culture { get; set; }
        
        private Hero _hero => Hero.MainHero;

        public Hero BaseHero => _hero;

        public HeroWrapper()
        {
            Clan = new ClanWrapper(_hero.Clan);
            Culture = new CultureWrapper(_hero.Culture);
        }
    }
}
