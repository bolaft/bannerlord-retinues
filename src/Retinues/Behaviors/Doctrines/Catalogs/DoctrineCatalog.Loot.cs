using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Configuration;
using Retinues.Interface.Services;

namespace Retinues.Behaviors.Doctrines.Catalogs
{
    /// <summary>
    /// Doctrines definitions for the Loot category.
    /// </summary>
    public static partial class DoctrineCatalog
    {
        public const string Category_Loot = "cat_loot";
        public const string LO_LionsShare = "doc_loot_lions_share";
        public const string LO_BattlefieldTithes = "doc_loot_battlefield_tithes";
        public const string LO_PragmaticScavengers = "doc_loot_pragmatic_scavengers";
        public const string LO_AncestralHeritage = "doc_loot_ancestral_heritage";

        public static Doctrine LionsShare => Doctrine.Get(LO_LionsShare);
        public static Doctrine BattlefieldTithes => Doctrine.Get(LO_BattlefieldTithes);
        public static Doctrine PragmaticScavengers => Doctrine.Get(LO_PragmaticScavengers);
        public static Doctrine AncestralHeritage => Doctrine.Get(LO_AncestralHeritage);

        public static DoctrineCategoryData CategoryLoot { get; } =
            new()
            {
                Id = Category_Loot,
                Name = L.T("doctrine_cat_loot", "Loot"),
                Doctrines =
                [
                    new DoctrineData
                    {
                        Id = LO_LionsShare,
                        Name = L.T("doctrine_loot_lions_share", "Lion's Share"),
                        Description = L.T(
                            "doctrine_loot_lions_share_desc",
                            "Hero kills count twice toward unlocking enemy gear."
                        ),
                        Sprite = "PerkFightingMadness",
                        Feats =
                        [
                            FeatCatalog.LS_CutTheHead,
                            FeatCatalog.LS_BloodPrice,
                            FeatCatalog.LS_HighValueTargets,
                        ],
                        Overridden = () => Settings.UnlockItemsThroughKills == false,
                        OverriddenHint = OverriddenByOption(Settings.UnlockItemsThroughKills),
                    },
                    new DoctrineData
                    {
                        Id = LO_BattlefieldTithes,
                        Name = L.T("doctrine_loot_battlefield_tithes", "Battlefield Tithes"),
                        Description = L.T(
                            "doctrine_loot_battlefield_tithes_desc",
                            "Allies contribute to equipment unlock progress."
                        ),
                        Sprite = "PerkBrawny",
                        Feats =
                        [
                            FeatCatalog.BT_TurnTheTide,
                            FeatCatalog.BT_SecondInCommand,
                            FeatCatalog.BT_AlliesFavor,
                        ],
                        Overridden = () => Settings.UnlockItemsThroughKills == false,
                        OverriddenHint = OverriddenByOption(Settings.UnlockItemsThroughKills),
                    },
                    new DoctrineData
                    {
                        Id = LO_PragmaticScavengers,
                        Name = L.T("doctrine_loot_pragmatic_scavengers", "Pragmatic Scavengers"),
                        Description = L.T(
                            "doctrine_loot_pragmatic_scavengers_desc",
                            "Gear can be unlocked from allied casualties."
                        ),
                        Sprite = "PerkTribesmen",
                        Feats =
                        [
                            FeatCatalog.PR_CostlyVictory,
                            FeatCatalog.PR_RescueMission,
                            FeatCatalog.PR_MarchTogether,
                        ],
                        Overridden = () => Settings.UnlockItemsThroughKills == false,
                        OverriddenHint = OverriddenByOption(Settings.UnlockItemsThroughKills),
                    },
                    new DoctrineData
                    {
                        Id = LO_AncestralHeritage,
                        Name = L.T("doctrine_loot_ancestral_heritage", "Ancestral Heritage"),
                        Description = L.T(
                            "doctrine_loot_ancestral_heritage_desc",
                            "Unlocks all clan culture items."
                        ),
                        Sprite = "PerkHoldTheLine",
                        Feats =
                        [
                            FeatCatalog.AN_CulturalTriumph,
                            FeatCatalog.AN_Homecoming,
                            FeatCatalog.AN_AncestralDuty,
                        ],
                        Overridden = () => Settings.EquipmentNeedsUnlocking == false,
                        OverriddenHint = OverriddenByOption(Settings.EquipmentNeedsUnlocking),
                    },
                ],
            };
    }
}
