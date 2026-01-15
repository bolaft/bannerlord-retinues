using Retinues.UI.Services;

namespace Retinues.Game.Doctrines.Catalogs
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
                            "20% rebate when equipping gear from your clan culture."
                        ),
                        Feats = [CP_HometownTournament, CP_ProudAndStrong, CP_KingSlayer],
                    },
                    new DoctrineData
                    {
                        Id = EQ_HonorGuard,
                        Name = L.T("doctrine_armory_honor_guard", "Honor Guard"),
                        Description = L.T(
                            "doctrine_armory_honor_guard_desc",
                            "+15% to total equipment value limit."
                        ),
                        Feats = [HG_BloodMoney, HG_PaidInFull, HG_GoldenLegion],
                    },
                    new DoctrineData
                    {
                        Id = EQ_Ironclad,
                        Name = L.T("doctrine_armory_ironclad", "Ironclad"),
                        Description = L.T(
                            "doctrine_armory_ironclad_desc",
                            "+15% to total equipment weight limit."
                        ),
                        Feats = [IR_HeavyKit, IR_Ironmen, IR_TailorMade],
                    },
                    new DoctrineData
                    {
                        Id = EQ_RoyalPatronage,
                        Name = L.T("doctrine_armory_royal_patronage", "Royal Patronage"),
                        Description = L.T(
                            "doctrine_armory_royal_patronage_desc",
                            "20% rebate when equipping gear from your kingdom culture."
                        ),
                        Feats = [RP_RoyalHost, RP_RoyalLevy, RP_RoyalStewardship],
                    },
                ],
            };
    }
}
