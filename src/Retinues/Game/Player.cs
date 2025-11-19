using System;
using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Game
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
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Factions                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the player's clan as a WFaction wrapper.
        /// </summary>
        public static WFaction GetClan(WCulture culture) => WFaction.GetClan(culture);

        /// <summary>
        /// Returns the player's kingdom as a WFaction wrapper, if leader.
        /// </summary>
        public static WFaction GetKingdom(WCulture culture) => WFaction.GetKingdom(culture);

        public static IEnumerable<WCharacter> Troops
        {
            get
            {
                foreach (var faction in WFaction.Factions)
                    foreach (var troop in faction.Troops)
                        yield return troop;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ IFaction ━━━━━━━ */

        public static Clan Clan => Hero.MainHero.Clan;
        public static Kingdom Kingdom => Hero.MainHero.Clan.Kingdom;

        /* ━━━━━━━ Wrappers ━━━━━━━ */

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

        /// <summary>
        /// Returns true if the player is a kingdom leader.
        /// </summary>
        public static bool IsKingdomLeader => Hero.MainHero.IsKingdomLeader;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Renown                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the player's clan renown.
        /// </summary>
        public static float Renown => Clan.Renown;

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
                if (Clan.PlayerClan == null)
                    return 0;
                return (int)Clan.PlayerClan.Influence;
            }
        }

        /// <summary>
        /// Changes the player's clan influence by the given amount.
        /// </summary>
        public static void ChangeInfluence(int amount)
        {
            if (Clan.PlayerClan == null)
                return;
            Clan.PlayerClan.Influence = Math.Max(0f, Influence + amount);
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
    }
}
