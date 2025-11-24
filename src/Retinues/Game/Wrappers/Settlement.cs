using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Game;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for Settlement, provides helpers for notables, garrison, culture, faction, and volunteer swapping.
    /// </summary>
    [SafeClass]
    public class WSettlement(Settlement settlement) : BaseFactionMember
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Static                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IEnumerable<WSettlement> All
        {
            get
            {
                foreach (var s in Settlement.All)
                    if (s != null)
                        yield return new WSettlement(s);
            }
        }

        public static IEnumerable<WSettlement> PlayerFactionSettlements
        {
            get
            {
                foreach (var s in All)
                {
                    var f = s.PlayerFaction;
                    if (f != null)
                        yield return s;
                }
            }
        }

        public static WSettlement Current
        {
            get
            {
                var s = Settlement.CurrentSettlement;
                if (s == null)
                    return null;
                return new WSettlement(s);
            }
        }

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

        public WHero Governor =>
            _settlement?.Town?.Governor != null ? new WHero(_settlement?.Town?.Governor) : null;

        public List<WNotable> Notables =>
            _settlement?.Notables.Where(n => n != null).Select(n => new WNotable(n, this)).ToList()
            ?? [];

        public WCulture Culture => new(_settlement?.Culture);

        public override WFaction Clan
        {
            get
            {
                var clan = _settlement?.OwnerClan;
                if (clan == null)
                    return null;

                if (clan == TaleWorlds.CampaignSystem.Clan.PlayerClan)
                    return Player.Clan;

                return new WFaction(clan);
            }
        }

        public override WFaction Kingdom
        {
            get
            {
                var clan = _settlement?.OwnerClan;
                var kingdom = clan?.Kingdom;
                if (kingdom == null)
                    return null;

                if (kingdom == Player.Kingdom?.Base)
                    return Player.Kingdom;

                return new WFaction(kingdom);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WParty MilitiaParty =>
            _settlement?.MilitiaPartyComponent?.MobileParty != null
                ? new WParty(_settlement?.MilitiaPartyComponent?.MobileParty)
                : null;

        public WParty GarrisonParty =>
            _settlement?.Town?.GarrisonParty != null
                ? new WParty(_settlement?.Town?.GarrisonParty)
                : null;

        public WCharacter MilitiaMelee =>
            PlayerFaction?.MilitiaMelee?.IsActive == true
                ? PlayerFaction?.MilitiaMelee
                : Culture?.MilitiaMelee;
        public WCharacter MilitiaMeleeElite =>
            PlayerFaction?.MilitiaMeleeElite?.IsActive == true
                ? PlayerFaction?.MilitiaMeleeElite
                : Culture?.MilitiaMeleeElite;
        public WCharacter MilitiaRanged =>
            PlayerFaction?.MilitiaRanged?.IsActive == true
                ? PlayerFaction?.MilitiaRanged
                : Culture?.MilitiaRanged;
        public WCharacter MilitiaRangedElite =>
            PlayerFaction?.MilitiaRangedElite?.IsActive == true
                ? PlayerFaction?.MilitiaRangedElite
                : Culture?.MilitiaRangedElite;

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

        public static void SwapAll(
            bool members,
            bool prisoners,
            bool skipGarrisons = false,
            bool skipMilitias = false
        )
        {
            foreach (var s in All)
            {
                if (!skipGarrisons)
                {
                    if (members)
                        s.GarrisonParty?.MemberRoster?.SwapTroops();
                    if (prisoners)
                        s.GarrisonParty?.PrisonRoster?.SwapTroops();
                }
                if (!skipMilitias)
                {
                    if (members)
                        s.MilitiaParty?.MemberRoster?.SwapTroops();
                    if (prisoners)
                        s.MilitiaParty?.PrisonRoster?.SwapTroops();
                }
            }
        }

        /// <summary>
        /// Swap all volunteers in notables to match the given faction.
        /// Optionally mixes clan/kingdom troops based on recruitment config.
        /// </summary>
        public void SwapVolunteers(WFaction faction = null)
        {
            if (faction == null)
                faction = PlayerFaction;
            if (faction == null)
                return;

            if (Config.RestrictToSameCultureSettlements && faction.Culture != Culture)
                return; // skip if culture doesn't match

            // Determine whether we should mix clan/kingdom volunteers here.
            WFaction secondaryFaction = null;
            float secondaryProportion = 0f;

            if (!Config.DisableKingdomTroops)
            {
                var clan = Clan;
                var kingdom = Kingdom;

                bool isPlayerClanFief = clan?.IsPlayerClan == true;
                bool isPlayerKingdomFief = kingdom?.IsPlayerKingdom == true;

                // 1) Player clan fief that is also in the player kingdom:
                //    base = clan, mix in kingdom using KingdomVolunteersInClanFiefsProportion.
                if (isPlayerClanFief && isPlayerKingdomFief && clan != null && kingdom != null)
                {
                    if (faction == clan)
                    {
                        secondaryFaction = kingdom;
                        secondaryProportion = Config.KingdomVolunteersInClanFiefsProportion;
                    }
                }
                // 2) Player kingdom fief that is NOT owned by the player clan:
                //    base = kingdom, mix in clan using ClanVolunteersInKingdomFiefsProportion.
                else if (
                    !isPlayerClanFief
                    && isPlayerKingdomFief
                    && kingdom != null
                    && Player.Clan != null
                )
                {
                    if (faction == kingdom)
                    {
                        secondaryFaction = Player.Clan;
                        secondaryProportion = Config.ClanVolunteersInKingdomFiefsProportion;
                    }
                }
                // 3) Remote recruitment (RestrictToOwnedSettlements == false):
                //    settlement is outside the player kingdom, but we are temporarily swapping
                //    volunteers to Player.Clan (Recruit Anywhere); still allow mixing in
                //    Player.Kingdom using KingdomVolunteersInClanFiefsProportion.
                else if (
                    !isPlayerKingdomFief
                    && Player.Clan != null
                    && Player.Kingdom != null
                    && faction == Player.Clan
                    && !Config.RestrictToOwnedSettlements
                )
                {
                    secondaryFaction = Player.Kingdom;
                    secondaryProportion = Config.KingdomVolunteersInClanFiefsProportion;
                }

                // Clamp just in case MCM ever misbehaves.
                if (secondaryProportion < 0f)
                    secondaryProportion = 0f;
                else if (secondaryProportion > 1f)
                    secondaryProportion = 1f;

                if (secondaryFaction == null || secondaryProportion <= 0f)
                {
                    secondaryFaction = null;
                    secondaryProportion = 0f;
                }
            }

            if (secondaryFaction != null)
            {
                Log.Debug(
                    $"Swapping volunteers in settlement '{this}' for faction '{faction}' with mix '{secondaryFaction}' (p={secondaryProportion:0.##})."
                );
            }
            else
            {
                Log.Debug($"Swapping volunteers in settlement '{this}' for faction '{faction}'.");
            }

            foreach (var notable in Notables)
            {
                try
                {
                    // Overload that supports primary + secondary faction with proportion.
                    notable.SwapVolunteers(faction, secondaryFaction, secondaryProportion);
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
