using System.Collections.Generic;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops;
using TaleWorlds.CampaignSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Regression tests for the revolt save-corruption fix and the scrub recovery command.
    /// These run a real scrub against the loaded campaign, so they should be run on a
    /// disposable test save.
    /// </summary>
    public static class RegressionTests
    {
        /// <summary>
        /// scrub_save must release an orphaned custom stub (not reachable from the player's
        /// faction trees) while leaving live troops untouched.
        /// </summary>
        [GameTest(
            "ScrubReleasesOrphanKeepsLive",
            "regression",
            "scrub_save releases orphan stubs and keeps the player's live troops"
        )]
        public static void ScrubReleasesOrphanKeepsLive(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            // A live troop that must survive the scrub.
            var live = Player.Clan?.RetinueElite;

            using var sandbox = new TestSandbox();

            // Fabricate an orphan: a custom stub registered active but not in any faction tree.
            var orphan = sandbox.NewStub();
            var vanilla = Player.Clan?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "Player culture has a basic root troop.");
            orphan.FillFrom(
                vanilla,
                keepUpgrades: false,
                keepEquipment: false,
                keepSkills: false
            );
            var orphanId = orphan.StringId;

            Tests.AssertTrue(
                WCharacter.ActiveStubIds.Contains(orphanId),
                "Orphan stub is active before the scrub."
            );

            FactionCheats.ScrubSave(new List<string>());

            Tests.AssertFalse(
                WCharacter.ActiveStubIds.Contains(orphanId),
                "Scrub released the orphan stub."
            );

            if (live != null)
            {
                Tests.AssertTrue(
                    live.IsActive,
                    "Scrub kept the player's live elite retinue active."
                );
            }
        }

        /// <summary>
        /// ClearStaleKingdomData must never discard data while the player actually leads a
        /// kingdom (those troops are legitimate).
        /// </summary>
        [GameTest(
            "StaleKingdomClearRespectsLeadership",
            "regression",
            "ClearStaleKingdomData does not drop data while leading a kingdom"
        )]
        public static void StaleKingdomClearRespectsLeadership(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var behavior = Campaign.Current.GetCampaignBehavior<FactionBehavior>();
            Tests.AssertNotNull(behavior, "FactionBehavior is registered.");

            bool cleared = behavior.ClearStaleKingdomData();

            if (Player.Kingdom != null)
            {
                Tests.AssertFalse(
                    cleared,
                    "Must not clear kingdom troop data while the player leads a kingdom."
                );
            }
        }
    }
}
