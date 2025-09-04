using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Campaign;

namespace CustomClanTroops.Logic
{
    public static class Player
    {
        // =========================================================================
        // Members
        // =========================================================================

        private static WClan _clan;
        public static WClan Clan
        {
            get
            {
                if (_clan == null)
                    _clan = new WClan(Hero.MainHero.Clan);
                return _clan;
            }
        }

        private static WCulture _culture;
        public static WCulture Culture
        {
            get
            {
                if (_culture == null)
                    _culture = new WCulture(Hero.MainHero.Culture);
                return _culture;
            }
        }

        // =========================================================================
        // Gold
        // =========================================================================

        public static int Gold => Hero.MainHero.Gold;

        public static void ChangeGold(int amount)
        {
            Hero.MainHero.ChangeHeroGold(amount);
        }
    }
}
