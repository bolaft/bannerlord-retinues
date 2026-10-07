using Retinues.Behaviors.Agents.Patches;
using Retinues.Behaviors.Volunteers.Patches;
using Retinues.Compatibility.Interops.Shokuho;
using Retinues.Framework.Modules.Dependencies.Core;

namespace Retinues.Tests.Cases
{
    public static class HarmonyDiscoveryTests
    {
        [GameTest("ExplicitPatchDiscovery", "runner", RequiresCampaign = false)]
        public static void ExplicitPatchDiscovery(GameTestContext ctx)
        {
            Tests.AssertTrue(HarmonyDependency.IsPatchContainer(typeof(BodyguardFormationPatch)),
                "Class-level Harmony annotations remain discoverable.");
            Tests.AssertTrue(HarmonyDependency.IsPatchContainer(typeof(VolunteerSwapForPlayerPatches)),
                "Method-level Harmony annotations remain discoverable.");
            Tests.AssertTrue(!HarmonyDependency.IsPatchContainer(typeof(GameTestContext)),
                "A test cleanup method is not a Harmony cleanup callback.");
            Tests.AssertTrue(!HarmonyDependency.IsPatchContainer(typeof(ShokuhoEquipmentPatcher)),
                "The optional manual interop patch is not applied by automatic discovery.");
        }
    }
}
