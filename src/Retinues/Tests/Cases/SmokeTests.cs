using Retinues.Features.Experience;
using Retinues.Features.Stocks;
using Retinues.Features.Unlocks;
using Retinues.Managers;
using Retinues.Troops;
using TaleWorlds.CampaignSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Smoke tests: the mod's wiring is in place and the managers fail safe on null inputs. These
    /// catch a broken SubModule registration or a missing null-guard without exercising deep logic.
    /// </summary>
    public static class SmokeTests
    {
        /// <summary>
        /// Harmony patches were applied and the core campaign behaviors are registered.
        /// </summary>
        [GameTest(
            "ModuleWiringRegistered",
            "smoke",
            "Harmony patches applied and core behaviors registered"
        )]
        public static void ModuleWiringRegistered(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            Tests.AssertTrue(
                Retinues.SubModule.HarmonyPatchesApplied,
                "Harmony patches were applied."
            );

            Tests.AssertNotNull(
                Campaign.Current.GetCampaignBehavior<FactionBehavior>(),
                "FactionBehavior is registered."
            );
            Tests.AssertNotNull(StocksBehavior.Instance, "StocksBehavior is registered.");
            Tests.AssertNotNull(UnlocksBehavior.Instance, "UnlocksBehavior is registered.");
            Tests.AssertNotNull(TroopXpBehavior.Instance, "TroopXpBehavior is registered.");
        }

        /// <summary>
        /// The manager entry points return safe defaults on null inputs rather than throwing.
        /// </summary>
        [GameTest(
            "ManagersNullSafe",
            "smoke",
            "Manager entry points return safe defaults on null inputs"
        )]
        public static void ManagersNullSafe(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            Tests.AssertEqual(0, SkillManager.SkillCapByTier(null), "SkillCapByTier(null) is 0.");
            Tests.AssertEqual(0, SkillManager.SkillTotalByTier(null), "SkillTotalByTier(null) is 0.");
            Tests.AssertEqual(
                0,
                RetinueManager.ConversionGoldCostPerUnit(null),
                "Conversion gold cost for null is 0."
            );
            Tests.AssertEqual(0, EquipmentManager.GetItemCost(null), "GetItemCost(null) is 0.");
            Tests.AssertFalse(
                UpgradeManager.CanAddUpgradeToTroop(null),
                "CanAddUpgradeToTroop(null) is false."
            );
        }
    }
}
