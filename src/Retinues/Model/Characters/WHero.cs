using Retinues.Model.Factions;
using TaleWorlds.CampaignSystem;

namespace Retinues.Model.Characters
{
    public class WHero(Hero @base) : WBase<WHero, Hero>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Character => WCharacter.Get(Base.CharacterObject);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Factions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WClan Clan => WClan.Get(Base.Clan);
        public WCulture Culture => WCulture.Get(Base.Culture);

        public IBaseFaction Faction
        {
            get
            {
                if (Base.MapFaction is Clan clan)
                    return WClan.Get(clan);

                if (Base.MapFaction is Kingdom kingdom)
                    return WKingdom.Get(kingdom);

                return null;
            }
        }
    }
}
