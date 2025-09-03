using TaleWorlds.CampaignSystem;
using CustomClanTroops.Game.Troops.Campaign;

namespace CustomClanTroops.Game
{
    public static class Player
    {
        // =========================================================================
        // Members
        // =========================================================================

        public static TroopClan Clan => new(Hero.MainHero.Clan);

        public static TroopCulture Culture => new(Hero.MainHero.Culture);

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
