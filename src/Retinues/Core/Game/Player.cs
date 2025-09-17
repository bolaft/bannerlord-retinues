using System;
using System.Collections.Generic;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Game
{
    public static class Player
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Components                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static WFaction _clan;

        public static WFaction Clan
        {
            get
            {
                _clan ??= new WFaction(Hero.MainHero.Clan);
                return _clan;
            }
        }

        private static WCulture _culture;

        public static WCulture Culture
        {
            get
            {
                _culture ??= new WCulture(Hero.MainHero.Culture);
                return _culture;
            }
        }

        private static WParty _party;

        public static WParty Party
        {
            get
            {
                _party ??= new WParty(MobileParty.MainParty);
                return _party;
            }
        }

        private static WCharacter _character;
        public static WCharacter Character
        {
            get
            {
                _character ??= new WCharacter(Hero.MainHero.CharacterObject);
                return _character;
            }
        }

        public static IFaction MapFaction => Hero.MainHero.MapFaction;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Attributes                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static string Name => Hero.MainHero.Name.ToString();

        public static bool IsFemale => Hero.MainHero.IsFemale;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Renown                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static float Renown => Hero.MainHero.Clan.Renown;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Gold                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int Gold => Hero.MainHero.Gold;

        public static void ChangeGold(int amount)
        {
            Hero.MainHero.ChangeHeroGold(amount);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Influence                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int Influence
        {
            get
            {
                if (TaleWorlds.CampaignSystem.Clan.PlayerClan == null)
                    return 0;
                return (int)TaleWorlds.CampaignSystem.Clan.PlayerClan.Influence;
            }
        }

        public static void ChangeInfluence(int amount)
        {
            if (TaleWorlds.CampaignSystem.Clan.PlayerClan == null)
                return;
            TaleWorlds.CampaignSystem.Clan.PlayerClan.Influence = Math.Max(0f, Influence + amount);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Kingdom                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static bool IsKingdomLeader => Hero.MainHero.IsKingdomLeader;

        private static WFaction _kingdom;

        public static WFaction Kingdom =>
            IsKingdomLeader ? _kingdom ??= new WFaction(Hero.MainHero.Clan.Kingdom) : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        public static bool IsArmyLeader => Party.Army?.LeaderParty?.StringId == Party.StringId;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void Clear()
        {
            _clan = null;
            _culture = null;
            _kingdom = null;
        }
    }
}
