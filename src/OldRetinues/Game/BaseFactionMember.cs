using Retinues.Configuration;
using Retinues.Game.Wrappers;
using Retinues.Utils;

namespace OldRetinues.Game
{
    /// <summary>
    /// Abstract base for faction-related wrappers, provides access to clan, kingdom, and player faction logic.
    /// </summary>
    [SafeClass]
    public abstract class BaseFactionMember : StringIdentifier
    {
        public abstract WFaction Clan { get; }
        public abstract WFaction Kingdom { get; }

        /// <summary>
        /// Gets the player faction, preferring clan or kingdom based on config.
        /// </summary>
        public WFaction PlayerFaction
        {
            get
            {
                // Set directly from Player static to avoid new instance without troop attributes
                var clan = Clan?.IsPlayerClan == true ? Player.Clan : null;
                var kingdom = Kingdom?.IsPlayerKingdom == true ? Player.Kingdom : null;

                if (Config.DisableKingdomTroops)
                    return clan;

                return clan ?? kingdom;
            }
        }
    }
}
