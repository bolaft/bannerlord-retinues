using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers.Base;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Game.Wrappers
{
    [SafeClass(SwallowByDefault = false)]
    public class WSettlement(Settlement settlement) : FactionObject
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

        public List<WNotable> Notables =>
            _settlement?.Notables.Where(n => n != null).Select(n => new WNotable(n)).ToList() ?? [];

        public WParty Garrison =>
            _settlement?.Town?.GarrisonParty != null
                ? new WParty(_settlement.Town.GarrisonParty)
                : null;

        public WCulture Culture => new(_settlement?.Culture);

        public override WFaction Clan
        {
            get
            {
                var clan = _settlement?.OwnerClan;
                if (clan != null)
                    return new WFaction(clan);
                return null;
            }
        }

        public override WFaction Kingdom =>
            Clan != null ? new WFaction(_settlement?.OwnerClan.Kingdom) : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter MilitiaMelee =>
            PlayerFaction?.MilitiaMelee.IsActive == true
                ? PlayerFaction.MilitiaMelee
                : Culture.MilitiaMelee;
        public WCharacter MilitiaMeleeElite =>
            PlayerFaction?.MilitiaMeleeElite.IsActive == true
                ? PlayerFaction.MilitiaMeleeElite
                : Culture.MilitiaMeleeElite;
        public WCharacter MilitiaRanged =>
            PlayerFaction?.MilitiaRanged.IsActive == true
                ? PlayerFaction.MilitiaRanged
                : Culture.MilitiaRanged;
        public WCharacter MilitiaRangedElite =>
            PlayerFaction?.MilitiaRangedElite.IsActive == true
                ? PlayerFaction.MilitiaRangedElite
                : Culture.MilitiaRangedElite;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void SwapVolunteers(WFaction faction = null)
        {
            if (faction == null)
                faction = PlayerFaction;
            if (faction == null)
                return;

            Log.Debug(
                $"Swapping volunteers in settlement '{StringId}' for faction '{faction.StringId}'"
            );

            foreach (var notable in Notables)
            {
                try
                {
                    notable.SwapVolunteers(faction);
                }
                catch (System.Exception ex)
                {
                    Log.Error(
                        $"Exception while processing notable {notable.StringId} in settlement {StringId}: {ex}"
                    );
                }
            }
        }
    }
}
