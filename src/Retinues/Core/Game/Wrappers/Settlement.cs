using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers.Base;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Game.Wrappers
{
    /// <summary>
    /// Wrapper for Settlement, provides helpers for notables, garrison, culture, faction, and volunteer swapping.
    /// </summary>
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
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsTown => _settlement?.IsTown == true;
        public bool IsVillage => _settlement?.IsVillage == true;
        public bool IsCastle => _settlement?.IsCastle == true;

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

        /// <summary>
        /// Returns a list of all items in the settlement's inventory, along with their counts.
        /// </summary>
        public List<(WItem item, int count)> ItemCounts()
        {
            var items = new List<(WItem item, int count)>();
            foreach (var e in _settlement.ItemRoster)
                items.Add((new WItem(e.EquipmentElement.Item), e.Amount));
            return items;
        }

        /// <summary>
        /// Swap all volunteers in notables to match the given faction.
        /// </summary>
        public void SwapVolunteers(WFaction faction = null)
        {
            if (faction == null)
                faction = PlayerFaction;
            if (faction == null)
                return;

            Log.Debug($"Swapping volunteers in settlement '{this}' for faction '{faction}'");

            foreach (var notable in Notables)
            {
                try
                {
                    notable.SwapVolunteers(faction);
                }
                catch (System.Exception ex)
                {
                    Log.Error(
                        $"Exception while processing notable {notable} in settlement {this}: {ex}"
                    );
                }
            }
        }
    }
}
