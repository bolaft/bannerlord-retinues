using System.Collections.Generic;
using Retinues.Framework.Runtime;
using TaleWorlds.Library;

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
            var doctrines = Catalogs.DoctrineCatalog.GetDoctrines();
            var lines = new List<string>(doctrines.Count + 8)
            {
                "Doctrines:",
                "  id | state | progress",
                "--------------------------------------------",
            };

            foreach (var doctrine in doctrines)
            {
                lines.Add(
                    $"  {doctrine.Id} | {doctrine.GetState()} | {doctrine.Progress}/{Doctrine.ProgressTarget}"
                );
            }

            return string.Join("\n", lines);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("doctrine_unlock", "retinues")]
        public static string DoctrineUnlock(List<string> args)
        {
            if (args.Count < 1)
                return "Usage: doctrine_unlock <doctrine_id>";

            var id = args[0];
            var doctrine = Doctrine.Get(id);

            if (doctrine == null)
                return $"Doctrine '{id}' not found.";

            if (doctrine.IsUnlocked)
                return $"Doctrine '{doctrine.Name}' ({doctrine.Id}) is already unlocked.";

            doctrine.Progress = Doctrine.ProgressTarget;

            return $"Doctrine '{doctrine.Name}' ({doctrine.Id}) unlocked.";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Feats                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("feats_list", "retinues")]
        public static string FeatsList(List<string> args)
        {
            var feats = Catalogs.DoctrineCatalog.GetFeats();

            var lines = new List<string>(feats.Count + 8)
            {
                "Feats:",
                "  id | repeatable | consumed | times | progress",
                "---------------------------------------------------------------",
            };

            foreach (var feat in feats)
            {
                lines.Add(
                    $"  {feat.Id} | {(feat.Repeatable ? "yes" : "no")} | {(feat.IsCompleted ? "yes" : "no")} | {feat.Progress}/{feat.Target}"
                );
            }

            return string.Join("\n", lines);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("feat_complete", "retinues")]
        public static string FeatComplete(List<string> args)
        {
            if (args.Count < 1)
                return "Usage: feat_complete <feat_id> [times]";

            var id = args[0];
            var feat = Feat.Get(id);

            if (feat == null)
                return $"Feat '{id}' not found.";

            if (feat.IsCompleted)
                return $"Feat '{feat.Name}' ({feat.Id}) is already completed.";

            feat.Progress = feat.Target;

            return $"Feat '{feat.Name}' ({feat.Id}) completed.";
        }
    }
}
