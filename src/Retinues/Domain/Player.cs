using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace Retinues.Domain
{
    /// <summary>
    /// Static helpers for accessing player-related game state and attributes.
    /// </summary>
    [SafeClass]
    public static class Player
    {
        private static Hero MainHero => TaleWorlds.CampaignSystem.Hero.MainHero;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Attributes                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static string Name => MainHero.Name.ToString();
        public static bool IsFemale => MainHero.IsFemale;
        public static bool IsRuler => MainHero.IsKingdomLeader;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WCulture Culture => WCulture.Get(MainHero.Culture);
        public static WClan Clan => WClan.Get(MainHero.Clan);
        public static WKingdom Kingdom => WKingdom.Get(MainHero.Clan?.Kingdom);
        public static WHero Hero => WHero.Get(MainHero);
        public static WParty Party =>
            MobileParty.MainParty != null ? new(MobileParty.MainParty) : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Location                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WSettlement CurrentSettlement =>
            WSettlement.Get(Party?.Base?.CurrentSettlement);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Map Faction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IBaseFaction MapFaction
        {
            get
            {
                var faction = MainHero.MapFaction;
                if (faction is Kingdom kingdom)
                    return WKingdom.Get(kingdom);
                if (faction is Clan clan)
                    return WClan.Get(clan);
                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Renown                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static float Renown => MainHero.Clan.Renown;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Gold                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int Gold => MainHero.Gold;

        /// <summary>
        /// Changes the player's gold by the given amount.
        /// </summary>
        public static void ChangeGold(int amount)
        {
            MainHero.ChangeHeroGold(amount);
        }

        /// <summary>
        /// Adds the given amount of gold to the player.
        /// </summary>
        public static void AddGold(int amount)
        {
            if (amount <= 0)
                return;
            ChangeGold(amount);
        }

        /// <summary>
        /// Tries to spend the given amount of gold from the player.
        /// </summary>
        public static bool TrySpendGold(int amount)
        {
            if (amount <= 0)
                return true;
            if (Gold < amount)
                return false;
            ChangeGold(-amount);
            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Influence                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the player's current influence.
        /// </summary>
        public static int Influence
        {
            get
            {
                if (TaleWorlds.CampaignSystem.Clan.PlayerClan == null)
                    return 0;
                return (int)TaleWorlds.CampaignSystem.Clan.PlayerClan.Influence;
            }
        }

        /// <summary>
        /// Changes the player's influence by the given amount.
        /// </summary>
        public static void ChangeInfluence(int amount)
        {
            if (TaleWorlds.CampaignSystem.Clan.PlayerClan == null)
                return;
            TaleWorlds.CampaignSystem.Clan.PlayerClan.Influence = Math.Max(0f, Influence + amount);
        }

        /// <summary>
        /// Adds the given amount of influence to the player.
        /// </summary>
        public static void AddInfluence(int amount)
        {
            if (amount <= 0)
                return;
            ChangeInfluence(amount);
        }

        /// <summary>
        /// Tries to spend the given amount of influence from the player.
        /// </summary>
        public static bool TrySpendInfluence(int amount)
        {
            if (amount <= 0)
                return true;
            if (Influence < amount)
                return false;
            ChangeInfluence(-amount);
            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IEnumerable<WCharacter> Troops
        {
            get
            {
                if (Clan != null)
                    foreach (var troop in Clan.Troops)
                        if (!troop.IsHero)
                            yield return troop;

                if (Kingdom != null)
                    foreach (var troop in Kingdom.Troops)
                        if (!troop.IsHero)
                            yield return troop;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cheats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("list_custom_troops", "retinues")]
        public static string ListTroops(List<string> args)
        {
            string result = string.Empty;

            result += $"Clan '{Clan?.Name}' (ID: {Clan?.StringId}):\n";

            foreach (var troop in Clan.Troops)
                if (!troop.IsHero)
                    result += $"  - {troop.Name} (ID: {troop.StringId})\n";

            if (Kingdom == null)
                return result;
            else
                result += "\n";

            result += $"Kingdom '{Kingdom?.Name}' (ID: {Kingdom?.StringId}):\n";

            foreach (var troop in Kingdom.Troops)
                if (!troop.IsHero)
                    result += $"  - {troop.Name} (ID: {troop.StringId})\n";

            return result;
        }
    }
}
