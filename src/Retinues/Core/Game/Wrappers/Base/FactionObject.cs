using Retinues.Core.Utils;

namespace Retinues.Core.Game.Wrappers.Base
{
    [SafeClass(SwallowByDefault = false)]
    public abstract class FactionObject : StringIdentifier
    {
        public abstract WFaction Clan { get; }

        public abstract WFaction Kingdom { get; }

        public WFaction PlayerFaction
        {
            get
            {
                var clan = Clan?.IsPlayerClan == true ? Clan : null;
                var kingdom = Kingdom?.IsPlayerKingdom == true ? Kingdom : null;

                if (Config.GetOption<bool>("ClanTroopsOverKingdomTroops"))
                    return clan ?? kingdom;
                else
                    return kingdom ?? clan;
            }
        }
    }
}
