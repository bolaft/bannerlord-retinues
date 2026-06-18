using System;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for wrapper computed properties. Roster composition counting is exercised on a
    /// throwaway dummy roster with a known mix of custom-elite and vanilla troops.
    /// </summary>
    public static class WrappersTests
    {
        /// <summary>
        /// WRoster counts and ratios reflect a known troop composition.
        /// </summary>
        [GameTest(
            "RosterCompositionCounts",
            "wrappers",
            "WRoster counts/ratios reflect a known custom-elite + vanilla composition"
        )]
        public static void RosterCompositionCounts(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: true, copyWholeTree: false);

            var customElite = faction.RootElite;
            var vanilla = faction.Culture?.RootBasic;
            Tests.AssertNotNull(customElite, "Custom elite root exists.");
            Tests.AssertNotNull(vanilla, "Vanilla template exists.");
            Tests.AssertTrue(customElite.IsElite, "The elite root is elite.");
            Tests.AssertTrue(customElite.IsCustom, "The elite root is custom.");

            var roster = new WRoster(TroopRoster.CreateDummyTroopRoster(), Player.Party);
            roster.AddTroop(customElite, 3);
            roster.AddTroop(vanilla, 7);

            Tests.AssertEqual(10, roster.HealthyCount, "Total head count is 10.");
            Tests.AssertEqual(3, roster.CustomCount, "Three custom troops.");
            Tests.AssertEqual(3, roster.EliteCount, "Three elite troops (the custom elites only).");
            Tests.AssertTrue(
                Math.Abs(roster.CustomRatio - 0.3f) < 0.001f,
                "Custom ratio is 30%."
            );
        }
    }
}
