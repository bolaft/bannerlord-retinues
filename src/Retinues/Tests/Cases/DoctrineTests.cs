using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Doctrines;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Behaviors.Doctrines.Definitions;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests that doctrine/feat runtime state does not leak between characters.
    /// The definitions are process-global singletons, so a reset must clear their acquired/progress
    /// state when a new character starts.
    /// </summary>
    public static class DoctrineTests
    {
        [GameTest(
            "DoctrineStateResetsForNewCharacter",
            "doctrines",
            "ResetRuntimeState clears acquired flags and progress so a prior character's doctrines don't leak"
        )]
        public static void DoctrineStateResetsForNewCharacter(GameTestContext ctx)
        {
            DoctrinesRegistry.EnsureRegistered();

            var doctrine = DoctrinesRegistry.GetDoctrines().FirstOrDefault();
            Tests.AssertNotNull(doctrine, "At least one doctrine is registered.");

            var feat = DoctrinesRegistry.GetFeats().FirstOrDefault();
            Tests.AssertNotNull(feat, "At least one feat is registered.");

            // This test mutates the process-global singletons and calls the global reset, so snapshot
            // ALL doctrine/feat state and restore it afterward — the suite may run inside a live
            // campaign whose real doctrine progress must not be wiped.
            var doctrineSnapshot = DoctrinesRegistry
                .GetDoctrines()
                .ToDictionary(d => d.Id, d => (d.IsAcquired, d.Progress));
            var featSnapshot = DoctrinesRegistry.GetFeats().ToDictionary(f => f.Id, f => f.Progress);

            try
            {
                // Simulate a previous character's unlocked doctrine and in-progress feat.
                doctrine.IsAcquired = true;
                doctrine.ForceSet(Doctrine.ProgressTarget);
                feat.ForceSet(feat.Target);

                // Starting a new character must wipe that leaked state.
                DoctrinesRegistry.ResetRuntimeState();

                Tests.AssertFalse(
                    doctrine.IsAcquired,
                    "Doctrine acquired flag is cleared on reset."
                );
                Tests.AssertEqual(0, doctrine.Progress, "Doctrine progress is cleared on reset.");
                Tests.AssertEqual(0, feat.Progress, "Feat progress is cleared on reset.");
            }
            finally
            {
                RestoreDoctrineState(doctrineSnapshot, featSnapshot);
            }
        }

        [GameTest(
            "RepeatableFeatWrapsAndCreditsWorth",
            "doctrines",
            "A repeatable feat credits its worth and wraps back to 0 so it can be completed again"
        )]
        public static void RepeatableFeatWrapsAndCreditsWorth(GameTestContext ctx)
        {
            DoctrinesRegistry.EnsureRegistered();

            // Defender of the City is the reported case: target 1, repeatable. Before the
            // repeatable-feat fix it completed without crediting worth and stuck at 1/1; after it,
            // each completion credits the worth and the progress wraps to 0 (which is why the
            // tracker legitimately shows 0/1 right after the completion popup).
            var feat = Feat.Get(FeatCatalog.SM_DefenderOfTheCity.Id);
            Tests.AssertNotNull(feat, "Defender of the City is registered.");
            Tests.AssertTrue(feat.Repeatable, "Defender of the City is repeatable.");

            var doctrine = feat.Doctrine;
            Tests.AssertNotNull(doctrine, "The feat belongs to a doctrine.");

            // Snapshot ALL doctrine/feat state (prerequisites get mutated too) and restore after.
            var doctrineSnapshot = DoctrinesRegistry
                .GetDoctrines()
                .ToDictionary(d => d.Id, d => (d.IsAcquired, d.Progress));
            var featSnapshot = DoctrinesRegistry.GetFeats().ToDictionary(f => f.Id, f => f.Progress);

            try
            {
                // Put the doctrine in progress: clear it and acquire its prerequisite chain.
                doctrine.IsAcquired = false;
                doctrine.ForceSet(0);
                feat.ForceSet(0);

                for (var p = doctrine.Prerequisite; p != null; p = p.Prerequisite)
                    p.IsAcquired = true;

                if (!doctrine.IsInProgress)
                    return; // Feat requirements disabled or doctrine overridden; nothing to verify.

                feat.Add();
                Tests.AssertEqual(
                    0,
                    feat.Progress,
                    "Progress wraps to 0 after completing, ready to be earned again."
                );
                Tests.AssertEqual(
                    feat.Worth,
                    doctrine.Progress,
                    "The completion credited the feat's worth to the doctrine."
                );

                feat.Add();
                Tests.AssertEqual(
                    feat.Worth * 2,
                    doctrine.Progress,
                    "A second completion credits the worth again."
                );
            }
            finally
            {
                RestoreDoctrineState(doctrineSnapshot, featSnapshot);
            }
        }

        private static void RestoreDoctrineState(
            Dictionary<string, (bool IsAcquired, int Progress)> doctrineSnapshot,
            Dictionary<string, int> featSnapshot
        )
        {
            foreach (var d in DoctrinesRegistry.GetDoctrines())
            {
                if (d == null || !doctrineSnapshot.TryGetValue(d.Id, out var s))
                    continue;

                d.IsAcquired = s.IsAcquired;
                d.ForceSet(s.Progress);
            }

            foreach (var f in DoctrinesRegistry.GetFeats())
            {
                if (f != null && featSnapshot.TryGetValue(f.Id, out var p))
                    f.ForceSet(p);
            }
        }
    }
}
