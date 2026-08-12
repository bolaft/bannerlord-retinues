#if BL13 || BL14
using Retinues.Editor.Integration.MapBar;
using TaleWorlds.Engine.GauntletUI;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// The map-bar Troops button layers are added to the shared vanilla brushes at runtime.
    /// Redefining those brushes in XML (the old approach) erased layers added by other mods,
    /// so the layers must be injected — and vanilla layers preserved — in code.
    /// </summary>
    public static class MapBarTests
    {
        [GameTest(
            "TroopsBrushLayersAreInjected",
            "ui",
            "EnsureApplied adds the troops layers to the shared map-bar brushes without touching vanilla layers"
        )]
        public static void TroopsBrushLayersAreInjected(GameTestContext ctx)
        {
            var factory = UIResourceManager.BrushFactory;
            if (factory == null)
                return; // No UI context in this session; nothing to verify.

            var icons = factory.GetBrush("MapBar.Left.Icons");
            var backgrounds = factory.GetBrush("MapBar.Left.Button.Backgrounds");
            if (icons == null || backgrounds == null)
                return; // Map-bar brushes not loaded in this session; nothing to verify.

            TroopsIcon.EnsureApplied();
            TroopsIcon.EnsureApplied(); // Idempotent: a second call must not duplicate or throw.

            Tests.AssertNotNull(icons.GetLayer("troops"), "Icons brush has the troops layer.");
            Tests.AssertNotNull(
                backgrounds.GetLayer("troops"),
                "Backgrounds brush has the troops layer."
            );
            Tests.AssertNotNull(
                icons.GetLayer("character_developer"),
                "Vanilla brush layers are preserved."
            );
        }
    }
}
#endif
