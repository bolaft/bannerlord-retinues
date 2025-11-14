using System.Collections.Generic;
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
    public class WCulture(CultureObject culture) : BaseFaction
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly CultureObject _culture = culture;
        public CultureObject Base => _culture;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override string Name => Base?.Name?.ToString();
        public override string StringId => Base?.StringId ?? Name; // Some cultures have no StringId?
        public override string BannerCodeText => null; // TODO
        public override uint Color => Base?.Color ?? 0;
        public override uint Color2 => Base?.Color2 ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

#if BL13
        public BannerImageIdentifier Image =>
            Base.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
        public ImageIdentifier ImageIdentifier =>
            Base.Banner != null ? new BannerImageIdentifier(Base.Banner) : null;
#else
        public BannerCode BannerCode => BannerCode.CreateFrom(Base.BannerKey);
        public ImageIdentifierVM Image => new(BannerCode);
        public ImageIdentifier ImageIdentifier => new(BannerCode);
#endif

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Roots                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter TryGet(CharacterObject co)
        {
            if (co == null)
                return null;

            return new WCharacter(co);
        }

        /* ━━━━━━━ Retinues ━━━━━━━ */

        public override WCharacter RetinueElite
        {
            get => null; // Retinues don't have a culture
            set => throw new System.NotImplementedException();
        }

        public override WCharacter RetinueBasic
        {
            get => null; // Retinues don't have a culture
            set => throw new System.NotImplementedException();
        }

        /* ━━━━━━━━ Regular ━━━━━━━ */

        public override WCharacter RootBasic
        {
            get => TryGet(Base.BasicTroop);
            set => throw new System.NotImplementedException();
        }

        public override WCharacter RootElite
        {
            get => TryGet(Base.EliteBasicTroop);
            set => throw new System.NotImplementedException();
        }

        /* ━━━━━━━━ Special ━━━━━━━ */

        public override WCharacter MilitiaMelee
        {
            get => TryGet(Base.MeleeMilitiaTroop);
            set => throw new System.NotImplementedException();
        }

        public override WCharacter MilitiaMeleeElite
        {
            get => TryGet(Base.MeleeEliteMilitiaTroop);
            set => throw new System.NotImplementedException();
        }

        public override WCharacter MilitiaRanged
        {
            get => TryGet(Base.RangedMilitiaTroop);
            set => throw new System.NotImplementedException();
        }

        public override WCharacter MilitiaRangedElite
        {
            get => TryGet(Base.RangedEliteMilitiaTroop);
            set => throw new System.NotImplementedException();
        }

        public override WCharacter Villager
        {
            get => TryGet(Base.Villager);
            set => throw new System.NotImplementedException();
        }

        public override WCharacter CaravanMaster
        {
            get => TryGet(Base.CaravanMaster);
            set => throw new System.NotImplementedException();
        }

        public override WCharacter CaravanGuard
        {
            get => TryGet(Base.CaravanGuard);
            set => throw new System.NotImplementedException();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Troop Lists                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override List<WCharacter> CivilianTroops =>
            GetActiveList(
                [
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
                ]
            );

        public override List<WCharacter> BanditTroops =>
            GetActiveList(
                [Base.BanditBandit, Base.BanditChief, Base.BanditBoss, Base.BanditRaider]
            );
    }
}
