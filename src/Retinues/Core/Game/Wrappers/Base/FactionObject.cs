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
                if (Config.GetOption<bool>("ClanTroopsOverKingdomTroops"))
                    return Clan ?? Kingdom;
                else
                    return Kingdom ?? Clan;
            }
        }
    }
}
