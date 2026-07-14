using System.Collections.Generic;
using System.Linq;
using Retinues.Behaviors.Doctrines;
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
