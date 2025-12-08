using Retinues.Model.Characters;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
# if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# else
using TaleWorlds.Core.ViewModelCollection;
# endif

namespace Retinues.Model.Factions
{
    public sealed class WCulture : BaseFaction<WCulture, CultureObject>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => Base.Name?.ToString() ?? string.Empty;

        public uint Color => Base.Color;

        public uint Color2 => Base.Color2;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

# if BL13
        public BannerImageIdentifierVM Image => new(Banner);

        public ImageIdentifier ImageIdentifier =>
            Base.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
#else
        public ImageIdentifierVM Image => new(BannerCode.CreateFrom(Base.BannerKey));

        public ImageIdentifier ImageIdentifier => new(BannerCode.CreateFrom(Base.BannerKey));
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Banner                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

# if BL13
        public Banner Banner => Base.Banner;
#else
        public Banner Banner => new(Base.BannerKey);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Characters                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Roots ━━━━━━━━ */

        public override WCharacter RootElite => WCharacter.Get(Base.EliteBasicTroop);
        public override WCharacter RootBasic => WCharacter.Get(Base.BasicTroop);

        /* ━━━━━━━ Militias ━━━━━━━ */

        public override WCharacter MeleeMilitiaTroop => WCharacter.Get(Base.MeleeMilitiaTroop);
        public override WCharacter MeleeEliteMilitiaTroop =>
            WCharacter.Get(Base.MeleeEliteMilitiaTroop);
        public override WCharacter RangedEliteMilitiaTroop =>
            WCharacter.Get(Base.RangedEliteMilitiaTroop);
        public override WCharacter RangedMilitiaTroop => WCharacter.Get(Base.RangedMilitiaTroop);

#if BL12
        public override WCharacter MilitiaArcher => WCharacter.Get(Base.MilitiaArcher);
#else
        public override WCharacter MilitiaArcher => null;
#endif

#if BL12
        public override WCharacter MilitiaSpearman => WCharacter.Get(Base.MilitiaSpearman);
#else
        public override WCharacter MilitiaSpearman => null;
#endif

#if BL12
        public override WCharacter MilitiaVeteranSpearman =>
            WCharacter.Get(Base.MilitiaVeteranSpearman);
#else
        public override WCharacter MilitiaVeteranSpearman => null;
#endif
        public override WCharacter MilitiaVeteranArcher =>
            WCharacter.Get(Base.MilitiaVeteranArcher);

        /* ━━━━━━━ Caravans ━━━━━━━ */

        public override WCharacter CaravanMaster => WCharacter.Get(Base.CaravanMaster);
        public override WCharacter CaravanGuard => WCharacter.Get(Base.CaravanGuard);

#if BL12
        public override WCharacter ArmedTrader => WCharacter.Get(Base.ArmedTrader);
#else
        public override WCharacter ArmedTrader => null;
#endif

        /* ━━━━━━━━ Bandits ━━━━━━━ */

        public override WCharacter BanditChief => WCharacter.Get(Base.BanditChief);
        public override WCharacter BanditRaider => WCharacter.Get(Base.BanditRaider);
        public override WCharacter BanditBandit => WCharacter.Get(Base.BanditBandit);
        public override WCharacter BanditBoss => WCharacter.Get(Base.BanditBoss);

        /* ━━━━━━━ Civilians ━━━━━━ */

        public override WCharacter TournamentMaster => WCharacter.Get(Base.TournamentMaster);
        public override WCharacter Villager => WCharacter.Get(Base.Villager);

        public override WCharacter PrisonGuard => WCharacter.Get(Base.PrisonGuard);
        public override WCharacter Guard => WCharacter.Get(Base.Guard);

        public override WCharacter Blacksmith => WCharacter.Get(Base.Blacksmith);
        public override WCharacter Weaponsmith => WCharacter.Get(Base.Weaponsmith);

        public override WCharacter Townswoman => WCharacter.Get(Base.Townswoman);
        public override WCharacter TownswomanInfant => WCharacter.Get(Base.TownswomanInfant);
        public override WCharacter TownswomanChild => WCharacter.Get(Base.TownswomanChild);
        public override WCharacter TownswomanTeenager => WCharacter.Get(Base.TownswomanTeenager);

        public override WCharacter VillageWoman => WCharacter.Get(Base.VillageWoman);
        public override WCharacter VillagerMaleChild => WCharacter.Get(Base.VillagerMaleChild);
        public override WCharacter VillagerMaleTeenager =>
            WCharacter.Get(Base.VillagerMaleTeenager);
        public override WCharacter VillagerFemaleChild => WCharacter.Get(Base.VillagerFemaleChild);
        public override WCharacter VillagerFemaleTeenager =>
            WCharacter.Get(Base.VillagerFemaleTeenager);

        public override WCharacter Townsman => WCharacter.Get(Base.Townsman);
        public override WCharacter TownsmanInfant => WCharacter.Get(Base.TownsmanInfant);
        public override WCharacter TownsmanChild => WCharacter.Get(Base.TownsmanChild);
        public override WCharacter TownsmanTeenager => WCharacter.Get(Base.TownsmanTeenager);

        public override WCharacter RansomBroker => WCharacter.Get(Base.RansomBroker);
        public override WCharacter GangleaderBodyguard => WCharacter.Get(Base.GangleaderBodyguard);
        public override WCharacter MerchantNotary => WCharacter.Get(Base.MerchantNotary);
        public override WCharacter ArtisanNotary => WCharacter.Get(Base.ArtisanNotary);
        public override WCharacter PreacherNotary => WCharacter.Get(Base.PreacherNotary);
        public override WCharacter RuralNotableNotary => WCharacter.Get(Base.RuralNotableNotary);
        public override WCharacter ShopWorker => WCharacter.Get(Base.ShopWorker);

        public override WCharacter Tavernkeeper => WCharacter.Get(Base.Tavernkeeper);
        public override WCharacter TavernGamehost => WCharacter.Get(Base.TavernGamehost);
        public override WCharacter Musician => WCharacter.Get(Base.Musician);
        public override WCharacter TavernWench => WCharacter.Get(Base.TavernWench);

        public override WCharacter Armorer => WCharacter.Get(Base.Armorer);
        public override WCharacter HorseMerchant => WCharacter.Get(Base.HorseMerchant);
        public override WCharacter Barber => WCharacter.Get(Base.Barber);
        public override WCharacter Merchant => WCharacter.Get(Base.Merchant);
        public override WCharacter Beggar => WCharacter.Get(Base.Beggar);
        public override WCharacter FemaleBeggar => WCharacter.Get(Base.FemaleBeggar);
        public override WCharacter FemaleDancer => WCharacter.Get(Base.FemaleDancer);

#if BL13
        public override WCharacter Shipwright => WCharacter.Get(Base.Shipwright);
#else
        public override WCharacter Shipwright => null;
#endif

#if BL12
        public override WCharacter Steward => WCharacter.Get(Base.Steward);
#else
        public override WCharacter Steward => null;
#endif
    }
}
