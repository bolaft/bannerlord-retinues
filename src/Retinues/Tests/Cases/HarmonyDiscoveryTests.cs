using Retinues.Features.Volunteers.Patches;
using Retinues.Mods.Shokuho;
using Retinues.Safety.Uninstall;

namespace Retinues.Tests.Cases
{
    public static class HarmonyDiscoveryTests
    {
        [GameTest("ExplicitPatchDiscovery", "runner", RequiresCampaign = false)]
        public static void ExplicitPatchDiscovery()
        {
            Tests.AssertTrue(SubModule.IsPatchContainer(typeof(VolunteerSwapForPlayer)),
                "Annotated recruitment patches must still be installed.");
            Tests.AssertFalse(SubModule.IsPatchContainer(typeof(UninstallBehavior)),
                "Uninstall preparation must never run as a Harmony Prepare hook at startup.");
            Tests.AssertFalse(SubModule.IsPatchContainer(typeof(GameTestContext)),
                "Test cleanup is not a Harmony callback.");
            Tests.AssertFalse(SubModule.IsPatchContainer(typeof(ShokuhoEquipmentPatcher)),
                "Manual compatibility patches must not be installed twice.");
        }
    }
}
