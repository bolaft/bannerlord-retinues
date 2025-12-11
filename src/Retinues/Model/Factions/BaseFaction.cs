using System.Collections.Generic;
using Retinues.Model.Characters;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
#if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Model.Factions
{
    public abstract class BaseFaction<TWrapper, TBase>(TBase @base)
        : WBase<TWrapper, TBase>(@base),
            IBaseFaction
        where TWrapper : BaseFaction<TWrapper, TBase>
        where TBase : MBObjectBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public abstract string Name { get; }
        public abstract uint Color { get; }
        public abstract uint Color2 { get; }
        public abstract Banner Banner { get; }

#if BL12
        public abstract BannerCode BannerCode { get; }
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

# if BL13
        public BannerImageIdentifierVM Image => new(Banner);
        public ImageIdentifier ImageIdentifier =>
            Banner != null ? new BannerImageIdentifier(Banner) : null;
#else
        public ImageIdentifierVM Image => new(BannerCode);
        public ImageIdentifier ImageIdentifier => new(BannerCode);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Characters                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Roots ━━━━━━━━ */

        public virtual WCharacter RootElite => null;
        public virtual WCharacter RootBasic => null;

        public List<WCharacter> RosterElite => RootElite != null ? RootElite.Tree : [];
        public List<WCharacter> RosterBasic => RootBasic != null ? RootBasic.Tree : [];

        /* ━━━━━━━━ Heroes ━━━━━━━━ */

        public virtual List<WHero> RosterHeroes => [];

        /* ━━━━━━━ Militias ━━━━━━━ */

        public virtual WCharacter MeleeMilitiaTroop => null;
        public virtual WCharacter MeleeEliteMilitiaTroop => null;
        public virtual WCharacter RangedEliteMilitiaTroop => null;
        public virtual WCharacter RangedMilitiaTroop => null;

        public virtual WCharacter MilitiaArcher => null;
        public virtual WCharacter MilitiaSpearman => null;
        public virtual WCharacter MilitiaVeteranSpearman => null;
        public virtual WCharacter MilitiaVeteranArcher => null;

        /* ━━━━━━━ Caravans ━━━━━━━ */

        public virtual WCharacter CaravanMaster => null;
        public virtual WCharacter CaravanGuard => null;
        public virtual WCharacter ArmedTrader => null;

        /* ━━━━━━━ Villagers ━━━━━━ */

        public virtual WCharacter Villager => null;

        /* ━━━━━━━━ Bandits ━━━━━━━ */

        public virtual WCharacter BanditChief => null;
        public virtual WCharacter BanditRaider => null;
        public virtual WCharacter BanditBandit => null;
        public virtual WCharacter BanditBoss => null;

        /* ━━━━━━━ Civilians ━━━━━━ */

        public virtual WCharacter TournamentMaster => null;

        public virtual WCharacter PrisonGuard => null;
        public virtual WCharacter Guard => null;

        public virtual WCharacter Blacksmith => null;
        public virtual WCharacter Weaponsmith => null;

        public virtual WCharacter Townswoman => null;
        public virtual WCharacter TownswomanInfant => null;
        public virtual WCharacter TownswomanChild => null;
        public virtual WCharacter TownswomanTeenager => null;

        public virtual WCharacter VillageWoman => null;
        public virtual WCharacter VillagerMaleChild => null;
        public virtual WCharacter VillagerMaleTeenager => null;
        public virtual WCharacter VillagerFemaleChild => null;
        public virtual WCharacter VillagerFemaleTeenager => null;

        public virtual WCharacter Townsman => null;
        public virtual WCharacter TownsmanInfant => null;
        public virtual WCharacter TownsmanChild => null;
        public virtual WCharacter TownsmanTeenager => null;

        public virtual WCharacter RansomBroker => null;
        public virtual WCharacter GangleaderBodyguard => null;
        public virtual WCharacter MerchantNotary => null;
        public virtual WCharacter ArtisanNotary => null;
        public virtual WCharacter PreacherNotary => null;
        public virtual WCharacter RuralNotableNotary => null;
        public virtual WCharacter ShopWorker => null;

        public virtual WCharacter Tavernkeeper => null;
        public virtual WCharacter TavernGamehost => null;
        public virtual WCharacter Musician => null;
        public virtual WCharacter TavernWench => null;

        public virtual WCharacter Armorer => null;
        public virtual WCharacter HorseMerchant => null;
        public virtual WCharacter Barber => null;
        public virtual WCharacter Merchant => null;
        public virtual WCharacter Beggar => null;
        public virtual WCharacter FemaleBeggar => null;
        public virtual WCharacter FemaleDancer => null;

        public virtual WCharacter Steward => null;
        public virtual WCharacter Shipwright => null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Rosters                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines if the given character is valid for inclusion in a roster.
        /// </summary>
        private static bool IsValid(WCharacter character)
        {
            if (character == null)
                return false;

            if (character.Age < 18)
                return false;

            return true;
        }

        /// <summary>
        /// Collects the given characters into a list, filtering out invalid ones.
        /// </summary>
        protected static List<WCharacter> Collect(params WCharacter[] characters)
        {
            var list = new List<WCharacter>(characters.Length);
            for (int i = 0; i < characters.Length; i++)
            {
                var c = characters[i];
                if (IsValid(c))
                    list.Add(c);
            }

            return list;
        }

        public virtual List<WCharacter> RosterRetinues => [];

        public virtual List<WCharacter> RosterMilitia =>
            Collect(
                MeleeMilitiaTroop,
                MeleeEliteMilitiaTroop,
                RangedMilitiaTroop,
                RangedEliteMilitiaTroop,
                MilitiaArcher,
                MilitiaSpearman,
                MilitiaVeteranSpearman,
                MilitiaVeteranArcher
            );

        public virtual List<WCharacter> RosterCaravan =>
            Collect(CaravanMaster, CaravanGuard, ArmedTrader);

        public virtual List<WCharacter> RosterVillager => Collect(Villager);

        public virtual List<WCharacter> RosterCivilian =>
            Collect(
                TournamentMaster,
                PrisonGuard,
                Guard,
                Blacksmith,
                Weaponsmith,
                Townswoman,
                TownswomanInfant,
                TownswomanChild,
                TownswomanTeenager,
                VillageWoman,
                VillagerMaleChild,
                VillagerMaleTeenager,
                VillagerFemaleChild,
                VillagerFemaleTeenager,
                Townsman,
                TownsmanInfant,
                TownsmanChild,
                TownsmanTeenager,
                RansomBroker,
                GangleaderBodyguard,
                MerchantNotary,
                ArtisanNotary,
                PreacherNotary,
                RuralNotableNotary,
                ShopWorker,
                Tavernkeeper,
                TavernGamehost,
                Musician,
                TavernWench,
                Armorer,
                HorseMerchant,
                Barber,
                Merchant,
                Beggar,
                FemaleBeggar,
                FemaleDancer,
                Steward,
                Shipwright
            );

        public virtual List<WCharacter> RosterBandit =>
            Collect(BanditChief, BanditRaider, BanditBandit, BanditBoss);
    }
}
