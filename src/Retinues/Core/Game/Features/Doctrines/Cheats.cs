using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.Library;

namespace Retinues.Core.Game.Features.Doctrines
{
    /// Console cheats for Retinues (enable cheats; open console; type "retinues.feat_list" etc.)
    /// Examples:
    ///     retinues.feat_list
    ///     retinues.feat_add MAA_1000EliteKills 25
    ///     retinues.feat_set LionsShare.LS_25PersonalKills 25 (full name or short name both work)
    ///     retinues.feat_unlock CH_TournamentOwnCultureTown
    ///     retinues.feat_unlock_all
    public static class Cheats
    {
        // -------------- Commands ----------------

        [CommandLineFunctionality.CommandLineArgumentFunction("feat_list", "retinues")]
        public static string FeatList(List<string> args)
        {
            var docs = DoctrineAPI.AllDoctrines();
            if (docs == null || docs.Count == 0) return "No doctrines discovered.";

            var sb = new StringBuilder();
            foreach (var d in docs.OrderBy(x => x.Column).ThenBy(x => x.Row))
            {
                var status = DoctrineAPI.GetDoctrineStatus(d.Key);
                sb.AppendLine($"[{TrimType(d.Key)}] {d.Name}  —  {status}");
                if (d.Feats == null || d.Feats.Count == 0)
                {
                    sb.AppendLine("  (no feats)");
                    continue;
                }

                foreach (var f in d.Feats)
                {
                    int prog = DoctrineAPI.GetFeatProgress(f.Key);
                    int tgt  = DoctrineAPI.GetFeatTarget(f.Key);
                    bool done = DoctrineAPI.IsFeatComplete(f.Key);
                    sb.AppendLine($"  - {TrimType(f.Key)} : {prog}/{tgt} {(done ? "[DONE]" : "")} — {f.Description}");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("feat_add", "retinues")]
        public static string FeatAdd(List<string> args)
        {
            if (args.Count < 1) return "Usage: retinues.feat_add <FeatNameOrType> [amount]";
            var t = ResolveFeatType(args[0], out var key, out var err);
            if (t == null) return err;

            int amount = 1;
            if (args.Count >= 2 && !int.TryParse(args[1], out amount)) return "amount must be an integer.";

            int after = DoctrineAPI.AdvanceFeat(key, amount);
            int tgt = DoctrineAPI.GetFeatTarget(key);
            return $"{TrimType(key)} advanced by {amount}. Now {after}/{tgt}.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("feat_set", "retinues")]
        public static string FeatSet(List<string> args)
        {
            if (args.Count < 2) return "Usage: retinues.feat_set <FeatNameOrType> <amount>";
            var t = ResolveFeatType(args[0], out var key, out var err);
            if (t == null) return err;

            if (!int.TryParse(args[1], out var amount)) return "amount must be an integer.";
            DoctrineAPI.SetFeatProgress(key, amount);
            int tgt = DoctrineAPI.GetFeatTarget(key);
            bool done = DoctrineAPI.IsFeatComplete(key);
            return $"{TrimType(key)} set to {amount}/{tgt} {(done ? "[DONE]" : "")}.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("feat_unlock", "retinues")]
        public static string FeatUnlock(List<string> args)
        {
            if (args.Count < 1) return "Usage: retinues.feat_unlock <FeatNameOrType>";
            var t = ResolveFeatType(args[0], out var key, out var err);
            if (t == null) return err;

            int tgt = DoctrineAPI.GetFeatTarget(key);
            DoctrineAPI.SetFeatProgress(key, tgt);
            return $"{TrimType(key)} marked complete.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("feat_unlock_all", "retinues")]
        public static string FeatUnlockAll(List<string> args)
        {
            var all = AllFeatKeys();
            int n = 0;
            foreach (var key in all)
            {
                int tgt = DoctrineAPI.GetFeatTarget(key);
                DoctrineAPI.SetFeatProgress(key, tgt);
                n++;
            }
            return $"Completed {n} feats.";
        }

        // -------------- Helpers ----------------

        private static IEnumerable<string> AllFeatKeys()
        {
            var docs = DoctrineAPI.AllDoctrines() ?? Array.Empty<DoctrineDef>();
            foreach (var d in docs)
                if (d.Feats != null)
                    foreach (var f in d.Feats)
                        yield return f.Key;
        }

        /// <summary>Resolve a feat by: full type name, short nested name, or case-insensitive suffix.</summary>
        private static Type ResolveFeatType(string token, out string featKey, out string error)
        {
            featKey = null; error = null;
            if (string.IsNullOrWhiteSpace(token)) { error = "Empty feat token."; return null; }

            // Build an index once per call (fast enough for console).
            var byKey = new Dictionary<string, Type>(StringComparer.Ordinal);
            var byShort = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // shortName -> fullKey

            foreach (var d in DoctrineAPI.AllDoctrines() ?? Array.Empty<DoctrineDef>())
            {
                if (d.Feats == null) continue;
                foreach (var f in d.Feats)
                {
                    var full = f.Key; // Type.FullName
                    var t = GetTypeByFullName(full);
                    if (t == null) continue;
                    byKey[full] = t;

                    var shortName = TrimType(full); // e.g. MAA_1000EliteKills
                    if (!byShort.ContainsKey(shortName))
                        byShort[shortName] = full;
                }
            }

            // 1) exact full name
            if (byKey.TryGetValue(token, out var exact))
            {
                featKey = token; return exact;
            }

            // 2) short name match (e.g. "MAA_1000EliteKills")
            if (byShort.TryGetValue(token, out var mapped))
            {
                featKey = mapped; return byKey[mapped];
            }

            // 3) suffix/contains search
            var hit = byKey.Keys.FirstOrDefault(k =>
                k.EndsWith(token, StringComparison.OrdinalIgnoreCase) ||
                TrimType(k).Equals(token, StringComparison.OrdinalIgnoreCase));

            if (hit != null)
            {
                featKey = hit; return byKey[hit];
            }

            error = $"Feat not found: '{token}'. Try 'retinues.feat_list' to see available feats.";
            return null;
        }

        private static string TrimType(string full)
        {
            if (string.IsNullOrEmpty(full)) return full;
            int i = full.LastIndexOf('.');
            var tail = i >= 0 ? full.Substring(i + 1) : full;
            // nested type shortener: Namespace.Outer+Inner -> Inner
            int j = tail.LastIndexOf('+');
            return j >= 0 ? tail.Substring(j + 1) : tail;
        }

        private static Type GetTypeByFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;

            // Fast path
            var t = Type.GetType(fullName, throwOnError: false);
            if (t != null) return t;

            // Search loaded assemblies
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = a.GetType(fullName, throwOnError: false);
                    if (t != null) return t;
                }
                catch { /* ignore dynamic loaders */ }
            }
            return null;
        }
    }
}
