using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace OldRetinues.Game
{
    /// <summary>
    /// Static helpers for accessing player-related game state and attributes.
    /// Provides wrappers for clan, kingdom, character, party, and resources.
    /// </summary>
    [SafeClass]
    public static class Player
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Reset                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Resets cached player state (clan, culture, character, kingdom).
        /// </summary>
        public static void Reset()
        {
            _culture = null;
            _character = null;
            _clan = null;
            _kingdom = null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static WFaction _clan;

        /// <summary>
        /// Returns the player's clan as a WFaction wrapper.
        /// </summary>
        public static WFaction Clan
        {
            get
            {
                _clan ??= new WFaction(Hero.MainHero.Clan);
                return _clan;
            }
        }

        private static WCulture _culture;

        /// <summary>
        /// Returns the player's culture as a WCulture wrapper.
        /// </summary>
        public static WCulture Culture
        {
            get
            {
                _culture ??= new WCulture(Hero.MainHero.Culture);
                return _culture;
            }
        }

        private static WCharacter _character;

        /// <summary>
        /// Returns the player's character as a WCharacter wrapper.
        /// </summary>
        public static WCharacter Character
        {
            get
            {
                _character ??= new WCharacter(Hero.MainHero.CharacterObject);
                return _character;
            }
        }

        /// <summary>
        /// Returns the player's main party as a WParty wrapper.
        /// </summary>
        public static WParty Party => new(MobileParty.MainParty);

        /// <summary>
        /// Returns the player's map faction.
        /// </summary>
        public static IFaction MapFaction => Hero.MainHero.MapFaction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Attributes                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the player's name.
        /// </summary>
        public static string Name => Hero.MainHero.Name.ToString();

        /// <summary>
        /// Returns true if the player is female.
        /// </summary>
        public static bool IsFemale => Hero.MainHero.IsFemale;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Renown                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the player's clan renown.
        /// </summary>
        public static float Renown => Hero.MainHero.Clan.Renown;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Gold                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the player's gold.
        /// </summary>
        public static int Gold => Hero.MainHero.Gold;

        /// <summary>
        /// Changes the player's gold by the given amount.
        /// </summary>
        public static void ChangeGold(int amount)
        {
            Hero.MainHero.ChangeHeroGold(amount);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Influence                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the player's clan influence.
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
        /// Changes the player's clan influence by the given amount.
        /// </summary>
        public static void ChangeInfluence(int amount)
        {
            if (TaleWorlds.CampaignSystem.Clan.PlayerClan == null)
                return;
            TaleWorlds.CampaignSystem.Clan.PlayerClan.Influence = Math.Max(0f, Influence + amount);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Kingdom                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the player is a kingdom leader.
        /// </summary>
        public static bool IsKingdomLeader => Hero.MainHero.IsKingdomLeader;

        /// <summary>
        /// Returns the player's kingdom as a WFaction wrapper, if leader.
        /// </summary>
        public static WFaction Kingdom =>
            IsKingdomLeader ? _kingdom ??= new WFaction(Hero.MainHero.Clan.Kingdom) : null;

        private static WFaction _kingdom;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Enumerates all custom troops for the player's clan and kingdom.
        /// </summary>
        public static IEnumerable<WCharacter> Troops
        {
            get
            {
                if (Clan != null)
                    foreach (var troop in Clan.Troops)
                        yield return troop;

                if (Kingdom != null)
                    foreach (var troop in Kingdom.Troops)
                        yield return troop;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Army                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the player is the leader of their army.
        /// </summary>
        public static bool IsArmyLeader => Party.Army?.LeaderParty?.StringId == Party.StringId;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Settlement                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static WSettlement CurrentSettlement =>
            Party?.Base?.CurrentSettlement != null
                ? new WSettlement(Party.Base.CurrentSettlement)
                : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Lists the IDs of all player troops. Usage: retinues.list_custom_troops
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("list_custom_troops", "retinues")]
        public static string ListCustomTroops(List<string> args)
        {
            var list = Troops.Select(t => $"{t.StringId}: {t.Name}").ToList();
            if (list.Count == 0)
                return "No active custom troops found.";

            return string.Join("\n", list);
        }
    }
}
