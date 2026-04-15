using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Domain;
using Retinues.Domain.Characters.Helpers;
using Retinues.Interface.Services;
using Retinues.Settings;

namespace Retinues.Behaviors.Doctrines.Catalogs
{
    /// <summary>
    /// Doctrines definitions for the Equipments category.
    /// </summary>
    public static partial class DoctrineCatalog
    {
        public const string Category_Equipments = "cat_equipments";
        public const string EQ_CulturalPride = "doc_armory_cultural_pride";
        public const string EQ_HonorGuard = "doc_armory_honor_guard";
        public const string EQ_Ironclad = "doc_armory_ironclad";
        public const string EQ_RoyalPatronage = "doc_armory_royal_patronage";

        public static Doctrine CulturalPride => Doctrine.Get(EQ_CulturalPride);
        public static Doctrine HonorGuard => Doctrine.Get(EQ_HonorGuard);
        public static Doctrine Ironclad => Doctrine.Get(EQ_Ironclad);
        public static Doctrine RoyalPatronage => Doctrine.Get(EQ_RoyalPatronage);

        public static DoctrineCategoryData CategoryEquipments { get; } =
            new()
            {
                Id = Category_Equipments,
                Name = L.T("doctrine_cat_equipments", "Equipments"),
                Doctrines =
                [
                    new DoctrineData
                    {
                        Id = EQ_CulturalPride,
                        Name = L.T("doctrine_armory_cultural_pride", "Cultural Pride"),
                        Description = L.T(
                            "doctrine_armory_cultural_pride_desc",
                            "-20% to clan culture equipment costs."
                        ),
                        Sprite = "PerkHardened",
                        PreviewCharacterId = DoctrinePreviewHelper.ClanTree(f => f.RosterElite),
                        Feats =
                        [
                            FeatCatalog.CP_KingSlayer,
                            FeatCatalog.CP_HometownTournament,
                            FeatCatalog.CP_ProudAndStrong,
                        ],
                        Overridden = () => Configuration.EquipmentCostsMoney == false,
                        OverriddenHint = OverriddenByOption(Configuration.EquipmentCostsMoney),
                    },
                    new DoctrineData
                    {
                        Id = EQ_HonorGuard,
                        Name = L.T("doctrine_armory_honor_guard", "Honor Guard"),
                        Description = L.T(
                            "doctrine_armory_honor_guard_desc",
                            "+15% to total equipment value limit."
                        ),
                        Sprite = "PerkFullBarding",
                        PreviewCharacterId = DoctrinePreviewHelper.ClanTree(f => f.RosterElite),
                        Feats =
                        [
                            FeatCatalog.HG_BloodMoney,
                            FeatCatalog.HG_PaidInFull,
                            FeatCatalog.HG_GoldenLegion,
                        ],
                        Overridden = () => Configuration.EquipmentValueLimit == false,
                        OverriddenHint = OverriddenByOption(Configuration.EquipmentValueLimit),
                    },
                    new DoctrineData
                    {
                        Id = EQ_Ironclad,
                        Name = L.T("doctrine_armory_ironclad", "Ironclad"),
                        Description = L.T(
                            "doctrine_armory_ironclad_desc",
                            "+15% to total equipment weight limit."
                        ),
                        Sprite = "PerkArmorPadding",
                        PreviewCharacterId = () => Player.Culture?.Armorer?.StringId,
                        PreviewCivilian = true,
                        Feats =
                        [
                            FeatCatalog.IR_HeavyKit,
                            FeatCatalog.IR_Ironmen,
                            FeatCatalog.IR_TailorMade,
                        ],
                        Overridden = () => Configuration.EquipmentWeightLimit == false,
                        OverriddenHint = OverriddenByOption(Configuration.EquipmentWeightLimit),
                    },
                    new DoctrineData
                    {
                        Id = EQ_RoyalPatronage,
                        Name = L.T("doctrine_armory_royal_patronage", "Royal Patronage"),
                        Description = L.T(
                            "doctrine_armory_royal_patronage_desc",
                            "-20% to kingdom culture equipment costs."
                        ),
                        Sprite = "PerkGold",
                        PreviewCharacterId = DoctrinePreviewHelper.KingdomTree(f => f.RosterElite),
                        Feats =
                        [
                            FeatCatalog.RP_RoyalHost,
                            FeatCatalog.RP_RoyalLevy,
                            FeatCatalog.RP_RoyalStewardship,
                        ],
                        Overridden = () => Configuration.EquipmentCostsMoney == false,
                        OverriddenHint = OverriddenByOption(Configuration.EquipmentCostsMoney),
                    },
                ],
            };
    }
}
