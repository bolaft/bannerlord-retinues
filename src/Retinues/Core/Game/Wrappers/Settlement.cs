using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Game.Wrappers
{
    [SafeClass(SwallowByDefault = false)]
    public class WSettlement(Settlement settlement) : StringIdentifier
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Settlement _settlement = settlement;

        public Settlement Base => _settlement;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => _settlement?.Name?.ToString();

        public override string StringId => _settlement?.StringId;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Members                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WParty Garrison =>
            _settlement?.Town?.GarrisonParty != null
                ? new WParty(_settlement.Town.GarrisonParty)
                : null;

        public WCulture Culture => new(_settlement?.Culture);

        public WFaction Faction
        {
            get
            {
                var clan = _settlement?.OwnerClan;
                var kingdom = clan?.Kingdom;

                bool inPlayerClan =
                    Player.Clan != null && clan != null && Player.Clan.StringId == clan.StringId;
                bool inPlayerKingdom =
                    Player.Kingdom != null
                    && kingdom != null
                    && Player.Kingdom.StringId == kingdom.StringId;

                if (Config.GetOption<bool>("ClanTroopsOverKingdomTroops"))
                {
                    if (inPlayerClan && clan != null)
                        return new WFaction(clan);
                    if (inPlayerKingdom && kingdom != null)
                        return new WFaction(kingdom);
                }
                else
                {
                    if (inPlayerKingdom && kingdom != null)
                        return new WFaction(kingdom);
                    if (inPlayerClan && clan != null)
                        return new WFaction(clan);
                }

                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter MilitiaMelee =>
            Faction?.MilitiaMelee.IsActive == true ? Faction.MilitiaMelee : Culture.MilitiaMelee;
        public WCharacter MilitiaMeleeElite =>
            Faction?.MilitiaMeleeElite.IsActive == true
                ? Faction.MilitiaMeleeElite
                : Culture.MilitiaMeleeElite;
        public WCharacter MilitiaRanged =>
            Faction?.MilitiaRanged.IsActive == true ? Faction.MilitiaRanged : Culture.MilitiaRanged;
        public WCharacter MilitiaRangedElite =>
            Faction?.MilitiaRangedElite.IsActive == true
                ? Faction.MilitiaRangedElite
                : Culture.MilitiaRangedElite;
    }
}
