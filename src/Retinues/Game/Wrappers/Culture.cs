using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
#if BL13
using TaleWorlds.Core.ImageIdentifiers;
#endif

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for CultureObject, exposing troop roots and militia for custom logic.
    /// </summary>
    [SafeClass]
    public class WCulture(CultureObject culture) : StringIdentifier, ITroopFaction
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly CultureObject _culture = culture;

        public CultureObject Base => _culture;

        public WCulture Culture => this; // Self-reference for interface

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => _culture?.Name?.ToString();

        public override string StringId => _culture?.StringId ?? Name; // Some cultures have no StringId?

        public uint Color => _culture?.Color ?? 0;

        public uint Color2 => _culture?.Color2 ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13
        public BannerImageIdentifier Image =>
            _culture.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
        public ImageIdentifier ImageIdentifier =>
            _culture.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
#else
        public BannerCode BannerCode => BannerCode.CreateFrom(Base.BannerKey);
        public ImageIdentifierVM Image => new(BannerCode);
        public ImageIdentifier ImageIdentifier => new(BannerCode);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the basic root troop for this culture.
        /// </summary>
        public WCharacter RootBasic =>
            _culture?.BasicTroop != null ? new(_culture.BasicTroop) : null;

        /// <summary>
        /// Gets the elite root troop for this culture.
        /// </summary>
        public WCharacter RootElite =>
            _culture?.EliteBasicTroop != null ? new(_culture.EliteBasicTroop) : null;

        /// <summary>
        /// Gets the melee militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaMelee =>
            _culture?.MeleeMilitiaTroop != null ? new(_culture.MeleeMilitiaTroop) : null;

        /// <summary>
        /// Gets the elite melee militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaMeleeElite =>
            _culture?.MeleeEliteMilitiaTroop != null ? new(_culture.MeleeEliteMilitiaTroop) : null;

        /// <summary>
        /// Gets the ranged militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaRanged =>
            _culture?.RangedMilitiaTroop != null ? new(_culture.RangedMilitiaTroop) : null;

        /// <summary>
        /// Gets the elite ranged militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaRangedElite =>
            _culture?.RangedEliteMilitiaTroop != null
                ? new(_culture.RangedEliteMilitiaTroop)
                : null;

        /// <summary>
        /// Gets the villager troop for this culture.
        /// </summary>
        public WCharacter Villager => _culture?.Villager != null ? new(_culture.Villager) : null;

        /// <summary>
        ///  Gets the caravan master troop for this culture.
        /// </summary>
        public WCharacter CaravanMaster =>
            _culture?.CaravanMaster != null ? new(_culture.CaravanMaster) : null;

        /// <summary>
        ///  Gets the caravan guard troop for this culture.
        /// </summary>
        public WCharacter CaravanGuard =>
            _culture?.CaravanGuard != null ? new(_culture.CaravanGuard) : null;

        public WCharacter RetinueElite => null;

        public WCharacter RetinueBasic => null;

        public List<WCharacter> Troops
        {
            get
            {
                var troops = new List<WCharacter>();
                foreach (var troop in RetinueTroops)
                    troops.Add(troop);
                foreach (var troop in EliteTroops)
                    troops.Add(troop);
                foreach (var troop in BasicTroops)
                    troops.Add(troop);
                foreach (var troop in MilitiaTroops)
                    troops.Add(troop);
                foreach (var troop in CaravanTroops)
                    troops.Add(troop);
                foreach (var troop in VillagerTroops)
                    troops.Add(troop);
                return troops;
            }
        }

        public List<WCharacter> RetinueTroops => [];

        public List<WCharacter> EliteTroops => [.. RootElite?.Tree];

        public List<WCharacter> BasicTroops => [.. RootBasic?.Tree];

        public List<WCharacter> MilitiaTroops
        {
            get
            {
                var list = new List<WCharacter>();
                if (MilitiaMelee != null)
                    list.Add(MilitiaMelee);
                if (MilitiaMeleeElite != null)
                    list.Add(MilitiaMeleeElite);
                if (MilitiaRanged != null)
                    list.Add(MilitiaRanged);
                if (MilitiaRangedElite != null)
                    list.Add(MilitiaRangedElite);
                return list;
            }
        }

        public List<WCharacter> CaravanTroops =>
            [
                .. new List<WCharacter> { CaravanGuard, CaravanMaster }.Where(t =>
                    t?.IsActive == true
                ),
            ];

        public List<WCharacter> VillagerTroops =>
            [.. new List<WCharacter> { Villager }.Where(t => t?.IsActive == true)];

        public List<WCharacter> CivilianTroops =>
            [
                .. new List<CharacterObject>
                {
                    Base.PrisonGuard,
                    Base.Guard,
# if BL12
                    Base.Steward,
# endif
                    Base.Blacksmith,
                    Base.Weaponsmith,
                    Base.Townswoman,
                    Base.TownswomanInfant,
                    Base.TownswomanChild,
                    Base.TownswomanTeenager,
                    Base.VillageWoman,
                    Base.VillagerMaleChild,
                    Base.VillagerMaleTeenager,
                    Base.VillagerFemaleChild,
                    Base.VillagerFemaleTeenager,
                    Base.Townsman,
                    Base.TownsmanInfant,
                    Base.TownsmanChild,
                    Base.TownsmanTeenager,
                    Base.RansomBroker,
                    Base.GangleaderBodyguard,
                    Base.MerchantNotary,
                    Base.ArtisanNotary,
                    Base.PreacherNotary,
                    Base.RuralNotableNotary,
                    Base.ShopWorker,
                    Base.Tavernkeeper,
                    Base.TavernGamehost,
                    Base.Musician,
                    Base.TavernWench,
                    Base.Armorer,
                    Base.HorseMerchant,
                    Base.Barber,
                    Base.Merchant,
                    Base.Beggar,
                    Base.FemaleBeggar,
                    Base.FemaleDancer,
                }
                    .Where(t => t != null)
                    .Select(t => new WCharacter(t))
                    .Where(w => w?.IsActive == true && w?.Age >= 18),
            ];

        public List<WCharacter> BanditTroops =>
            [
                .. new List<CharacterObject>
                {
                    Base.BanditBandit,
                    Base.BanditChief,
                    Base.BanditBoss,
                    Base.BanditRaider,
                }
                    .Where(t => t != null)
                    .Select(t => new WCharacter(t))
                    .Where(w => w?.IsActive == true && w?.Age >= 18),
            ];
    }
}
