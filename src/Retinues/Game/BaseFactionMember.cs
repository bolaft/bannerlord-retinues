using Retinues.Configuration;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game
{
    public abstract class BaseFactionMember(Clan clan) : StringIdentifier
    {
        /* ━━━━ Clan & Kingdom ━━━━ */

        public virtual Clan Clan => clan;
        public virtual Kingdom Kingdom => Clan?.Kingdom;

        /* ━━━━━━━━ Culture ━━━━━━━ */

        private WCulture _culture;
        public virtual WCulture Culture => _culture ??= new(Clan?.Culture);


        /* ━━━━ Player Faction ━━━━ */

        public bool IsPlayerClan => Clan != null && Clan == Player.Clan;
        public bool IsPlayerKingdom => Kingdom != null && Kingdom == Player.Kingdom;

        public WFaction PlayerClan => IsPlayerClan ? WFaction.GetClan(Culture) : null;
        public WFaction PlayerKingdom => IsPlayerKingdom ? WFaction.GetKingdom(Culture) : null;


        /// <summary>
        /// Gets the player faction, preferring clan or kingdom based on config.
        /// </summary>
        public WFaction PlayerFaction
        {
            get
            {
                if (Config.ClanTroopsOverKingdomTroops)
                    return PlayerClan ?? PlayerKingdom;
                else
                    return PlayerKingdom ?? PlayerClan;
            }
        }
    }
}
