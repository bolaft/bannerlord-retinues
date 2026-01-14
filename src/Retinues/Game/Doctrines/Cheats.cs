using System;
using System.Collections.Generic;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    [SafeClass]
    public static class DoctrinesCheats
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Doctrines                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("doctrines_list", "retinues")]
        public static string DoctrinesList(List<string> args)
        {
            DoctrinesCatalog.EnsureBuilt();

            var lines = new List<string>(DoctrinesCatalog.Doctrines.Count + 8)
            {
                "Doctrines:",
                "  id | state | progress",
                "--------------------------------------------",
            };

            foreach (var kvp in DoctrinesCatalog.Doctrines)
            {
                var id = kvp.Key;
                var state = DoctrinesAPI.GetState(id);
                var progress = DoctrinesAPI.GetProgress(id);
                lines.Add($"  {id} | {state} | {progress}/100");
            }

            return string.Join("\n", lines);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("doctrine_unlock", "retinues")]
        public static string DoctrineUnlock(List<string> args)
        {
            if (args.Count < 1)
                return "Usage: doctrine_unlock <doctrine_id>";

            var doctrineId = args[0];

            if (!DoctrinesCatalog.TryGetDoctrine(doctrineId, out var def) || def == null)
                return $"Doctrine '{doctrineId}' not found.";

            if (!TryGetBehavior(out var behavior, out var err))
                return err;

            var dict = Reflection.GetFieldValue<Dictionary<string, int>>(
                behavior,
                "_doctrineProgress"
            );
            dict[doctrineId] = DoctrineDefinition.UnlockProgressTarget;

            return $"Doctrine '{doctrineId}' unlocked (progress 100/100).";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("doctrine_reset", "retinues")]
        public static string DoctrineReset(List<string> args)
        {
            if (args.Count < 1)
                return "Usage: doctrine_reset <doctrine_id>";

            var doctrineId = args[0];

            if (!DoctrinesCatalog.TryGetDoctrine(doctrineId, out var def) || def == null)
                return $"Doctrine '{doctrineId}' not found.";

            if (!TryGetBehavior(out var behavior, out var err))
                return err;

            var progress = Reflection.GetFieldValue<Dictionary<string, int>>(
                behavior,
                "_doctrineProgress"
            );
            var acquired = Reflection.GetFieldValue<Dictionary<string, bool>>(
                behavior,
                "_doctrineAcquired"
            );

            progress[doctrineId] = 0;
            acquired[doctrineId] = false;

            return $"Doctrine '{doctrineId}' reset (progress 0/100, not acquired).";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("doctrines_reset_all", "retinues")]
        public static string DoctrinesResetAll(List<string> args)
        {
            if (!TryGetBehavior(out var behavior, out var err))
                return err;

            var progress = Reflection.GetFieldValue<Dictionary<string, int>>(
                behavior,
                "_doctrineProgress"
            );
            var acquired = Reflection.GetFieldValue<Dictionary<string, bool>>(
                behavior,
                "_doctrineAcquired"
            );

            progress.Clear();
            acquired.Clear();

            return "All doctrines reset (progress/acquired cleared).";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Feats                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("feats_list", "retinues")]
        public static string FeatsList(List<string> args)
        {
            DoctrinesCatalog.EnsureBuilt();

            var lines = new List<string>(DoctrinesCatalog.Feats.Count + 8)
            {
                "Feats:",
                "  id | repeatable | consumed | times | progress",
                "---------------------------------------------------------------",
            };

            foreach (var kvp in DoctrinesCatalog.Feats)
            {
                var id = kvp.Key;
                var def = kvp.Value;

                var repeatable = def.Repeatable;
                var consumed = FeatsAPI.IsCompleted(id);
                var times = FeatsAPI.GetTimesCompleted(id);
                var prog = FeatsAPI.GetProgress(id);

                lines.Add(
                    $"  {id} | {(repeatable ? "yes" : "no")} | {(consumed ? "yes" : "no")} | {times} | {prog}/{def.Target}"
                );
            }

            return string.Join("\n", lines);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("feat_complete", "retinues")]
        public static string FeatComplete(List<string> args)
        {
            if (args.Count < 1)
                return "Usage: feat_complete <feat_id> [times]";

            var featId = args[0];

            if (!DoctrinesCatalog.TryGetFeat(featId, out var feat) || feat == null)
                return $"Feat '{featId}' not found.";

            var times = 1;
            if (args.Count >= 2 && !TryParseInt(args[1], out times))
                return "Invalid times.";
            if (times < 1)
                times = 1;

            // Uses the real rules.
            var done = 0;
            for (var i = 0; i < times; i++)
            {
                if (FeatsAPI.TryComplete(featId))
                    done++;
                else
                    break;
            }

            return done > 0
                ? $"Feat '{featId}' completed {done} time(s)."
                : $"Feat '{featId}' could not be completed (no linked in-progress doctrine, or consumed).";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Internals                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool TryGetBehavior(out DoctrinesBehavior behavior, out string error)
        {
            behavior = null;
            error = null;

            if (!DoctrinesBehavior.TryGetInstance(out behavior) || behavior == null)
            {
                error = "Error: DoctrinesBehavior is not registered in the current campaign.";
                return false;
            }

            return true;
        }

        private static bool TryParseInt(string s, out int value)
        {
            return int.TryParse(s, out value);
        }

        private static int Clamp(int v, int min, int max)
        {
            if (v < min)
                return min;
            if (v > max)
                return max;
            return v;
        }
    }
}
