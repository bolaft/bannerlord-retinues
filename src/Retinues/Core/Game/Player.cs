using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Game
{
    public static class Player
    {
        // =========================================================================
        // Members
        // =========================================================================

        public static string Name => Hero.MainHero.Name.ToString();

        public static bool IsFemale => Hero.MainHero.IsFemale;

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

        private static WParty _party;

        public static WParty Party
        {
            get
            {
                _party ??= new WParty(MobileParty.MainParty);
                return _party;
            }
        }

        // =========================================================================
        // Renown
        // =========================================================================

        public static float Renown => Hero.MainHero.Clan.Renown;

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

        // =========================================================================
        // Public API
        // =========================================================================

        public static void Clear()
        {
            _clan = null;
            _culture = null;
            _kingdom = null;
        }
    }
}
