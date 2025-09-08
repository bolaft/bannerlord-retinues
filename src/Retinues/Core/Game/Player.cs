using TaleWorlds.CampaignSystem;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Game
{
    public static class Player
    {
        // =========================================================================
        // Members
        // =========================================================================

        private static WFaction _clan;

        public static WFaction Clan
        {
            get
            {
                _clan ??= new WFaction(Hero.MainHero.Clan);
                return _clan;
            }
        }

        private static WCulture _culture;

        public static WCulture Culture
        {
            get
            {
                _culture ??= new WCulture(Hero.MainHero.Culture);
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

        // =========================================================================
        // Kingdom
        // =========================================================================

        public static bool IsKingdomLeader => Hero.MainHero.IsKingdomLeader;

        private static WFaction _kingdom;

        public static WFaction Kingdom => IsKingdomLeader ? _kingdom ??= new WFaction(Hero.MainHero.Clan.Kingdom) : null;
    }
}
