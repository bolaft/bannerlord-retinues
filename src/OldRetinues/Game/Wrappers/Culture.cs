using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
# if BL13
using TaleWorlds.Core.ImageIdentifiers;
# endif

namespace OldRetinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for CultureObject, exposing troop roots and militia for custom logic.
    /// </summary>
    [SafeClass]
    public class WCulture(CultureObject culture) : BaseBannerFaction
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Static                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static IEnumerable<WCulture> All
        {
            get
            {
                foreach (
                    var culture in MBObjectManager
                        .Instance.GetObjectTypeList<CultureObject>()
                        ?.OrderBy(c => c?.Name?.ToString())
                )
                    if (culture != null)
                        yield return new WCulture(culture);
            }
        }

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
        public override uint Color => Base?.Color ?? 0;
        public override uint Color2 => Base?.Color2 ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Banner                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override Banner BaseBanner
        {
            get
            {
                if (Base == null)
                    return null;

#if BL13
                return Base.Banner;
#else
                var bannerKey = Base.BannerKey;
                if (string.IsNullOrEmpty(bannerKey))
                    return null;

                return new Banner(bannerKey);
#endif
            }
        }

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

        /* ━━━━━━━━ Regular ━━━━━━━ */

        public override WCharacter RootBasic => TryGet(Base.BasicTroop);
        public override WCharacter RootElite => TryGet(Base.EliteBasicTroop);

        /* ━━━━━━━━ Special ━━━━━━━ */

        public override WCharacter MilitiaMelee => TryGet(Base.MeleeMilitiaTroop);
        public override WCharacter MilitiaMeleeElite => TryGet(Base.MeleeEliteMilitiaTroop);
        public override WCharacter MilitiaRanged => TryGet(Base.RangedMilitiaTroop);
        public override WCharacter MilitiaRangedElite => TryGet(Base.RangedEliteMilitiaTroop);
        public override WCharacter Villager => TryGet(Base.Villager);
        public override WCharacter CaravanMaster => TryGet(Base.CaravanMaster);
        public override WCharacter CaravanGuard => TryGet(Base.CaravanGuard);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Troop Lists                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override List<WCharacter> MercenaryTroops
        {
            get
            {
                var mercenaries = new List<WCharacter>();

                foreach (var root in Base.BasicMercenaryTroops)
                {
                    var troop = TryGet(root);
                    if (troop != null)
                        foreach (var t in troop.Tree)
                            mercenaries.Add(t);
                }

                return mercenaries;
            }
        }

        public override List<WCharacter> BanditTroops =>
            GetActiveList(
                [Base.BanditBandit, Base.BanditChief, Base.BanditBoss, Base.BanditRaider]
            );

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
    }
}
