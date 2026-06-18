using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Safety.Sanitizer;
using Retinues.Troops;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.Roster;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the roster sanitizer (used on load and for the uninstall purge) and version
    /// helpers. Sanitizing runs against throwaway dummy rosters so the player's party is safe.
    /// </summary>
    public static class SafetyTests
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Sanitizer                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// With replaceAllCustom, a custom troop is replaced by a non-custom fallback while the
        /// head count is preserved (the uninstall-purge path).
        /// </summary>
        [GameTest(
            "SanitizerForcesCustomReplacement",
            "safety",
            "replaceAllCustom swaps a custom troop for a fallback and preserves head count"
        )]
        public static void SanitizerForcesCustomReplacement(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var faction = sandbox.NewFaction();
            Tests.AssertNotNull(faction, "A non-player faction with troop roots is available.");
            TroopBuilder.CreateTroops(faction, isElite: false, copyWholeTree: false);

            var custom = faction.RootBasic;
            Tests.AssertNotNull(custom, "Custom basic root exists.");
            Tests.AssertTrue(custom.IsCustom, "The troop is custom.");

            var roster = new WRoster(TroopRoster.CreateDummyTroopRoster(), Player.Party);
            roster.AddTroop(custom, 4);
            Tests.AssertEqual(4, roster.CountOf(custom), "Custom troop was added.");

            PartySanitizer.SanitizeRoster(roster.Base, party: null, replaceAllCustom: true);

            Tests.AssertEqual(
                0,
                roster.CountOf(custom),
                "The custom troop was replaced by the forced sanitize."
            );
            Tests.AssertEqual(
                4,
                roster.HealthyCount,
                "Head count is preserved via the fallback troop."
            );
        }

        /// <summary>
        /// A normal sanitize (not forcing custom replacement) leaves valid vanilla troops alone.
        /// </summary>
        [GameTest(
            "SanitizerKeepsValidTroops",
            "safety",
            "A normal sanitize leaves valid vanilla troops untouched"
        )]
        public static void SanitizerKeepsValidTroops(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var vanilla = sandbox.NewFaction()?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "A valid vanilla troop is available.");

            var roster = new WRoster(TroopRoster.CreateDummyTroopRoster(), Player.Party);
            roster.AddTroop(vanilla, 6);

            PartySanitizer.SanitizeRoster(roster.Base, party: null, replaceAllCustom: false);

            Tests.AssertEqual(
                6,
                roster.CountOf(vanilla),
                "A valid vanilla troop is left untouched by the sanitizer."
            );
            Tests.AssertEqual(6, roster.HealthyCount, "Head count is unchanged.");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Version                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// The Bannerlord version flags are mutually consistent.
        /// </summary>
        [GameTest(
            "BannerlordVersionConsistent",
            "safety",
            "Bannerlord version flags are mutually consistent"
        )]
        public static void BannerlordVersionConsistent(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            Tests.AssertTrue(
                BannerlordVersion.Version.Major >= 1,
                "Bannerlord major version is at least 1."
            );

            if (BannerlordVersion.Is12())
                Tests.AssertFalse(BannerlordVersion.IsAtLeast14(), "1.2 is not >= 1.4.");
            if (BannerlordVersion.Is13())
                Tests.AssertFalse(BannerlordVersion.IsAtLeast14(), "1.3 is not >= 1.4.");
            if (BannerlordVersion.IsAtLeast14())
            {
                Tests.AssertFalse(BannerlordVersion.Is12(), ">= 1.4 excludes 1.2.");
                Tests.AssertFalse(BannerlordVersion.Is13(), ">= 1.4 excludes 1.3.");
            }
        }
    }
}
