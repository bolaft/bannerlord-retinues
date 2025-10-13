using Retinues.Configuration;
using Retinues.Utils;

namespace Retinues.Game.Wrappers.Base
{
    /// <summary>
    /// Abstract base for faction-related wrappers, provides access to clan, kingdom, and player faction logic.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
    public abstract class FactionObject : StringIdentifier
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
                var clan = Clan?.IsPlayerClan == true ? Clan : null;
                var kingdom = Kingdom?.IsPlayerKingdom == true ? Kingdom : null;

                if (Config.ClanTroopsOverKingdomTroops)
                    return clan ?? kingdom;
                else
                    return kingdom ?? clan;
            }
        }
    }
}
