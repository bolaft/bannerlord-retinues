using TaleWorlds.CampaignSystem;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class HeroWrapper
    {
        public static ClanWrapper Clan => new ClanWrapper(Hero.MainHero.Clan);

        public static CultureWrapper Culture => new CultureWrapper(Hero.MainHero.Culture);

        public static int Gold => Hero.MainHero.Gold;

        public static void ChangeGold(int amount)
        {
            Hero.MainHero.ChangeHeroGold(amount);
        }
    }
}
