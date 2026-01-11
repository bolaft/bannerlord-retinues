using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Utilities;

namespace Retinues.Framework.Model.Exports
{
    public static partial class MImportExport
    {
        public static bool TryApplyCharacterExport(
            WCharacter target,
            CharacterExportEntry entry,
            out string error
        )
        {
            error = null;

            try
            {
                if (target == null)
                {
                    error = "target troop is null.";
                    return false;
                }

                if (entry == null || string.IsNullOrWhiteSpace(entry.PayloadXml))
                {
                    error = "missing export payload.";
                    return false;
                }

                var existing = target.UpgradeTargets?.ToList() ?? [];

                var rewritten = RewriteCharacterPayload(
                    entry.PayloadXml,
                    keepUpgradeTargets: false
                );
                rewritten = ForceCharacterIdentity(rewritten, target.StringId);

                target.Deserialize(rewritten);
                target.UpgradeTargets = existing;

                WCharacter.InvalidateTroopSourceCaches();

                Log.Debug(
                    $"Applied character export to '{target.StringId}' (export source='{entry.SourceId ?? "unknown"}')."
                );
                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "TryApplyCharacterExport failed.");
                error = ex.Message ?? "unknown error.";
                return false;
            }
        }

        public static bool TryApplyFactionExport(
            IBaseFaction target,
            FactionExportData data,
            out ImportReport report,
            out string error
        )
        {
            report = new ImportReport();
            error = null;

            try
            {
                if (target == null)
                {
                    error = "target faction is null.";
                    return false;
                }

                if (data == null)
                {
                    error = "export data is null.";
                    return false;
                }

                // Do NOT apply faction payload here (prevents wiping target-only rosters).
                var groups = data
                    .Troops.Where(t => t != null)
                    .GroupBy(t =>
                    {
                        var k = t.RosterKey ?? string.Empty;
                        return string.IsNullOrWhiteSpace(k) ? RAll : k.Trim().ToLowerInvariant();
                    })
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                int imported = 0;
                int skippedTroops = 0;
                int skippedRosters = 0;

                imported += ApplyRosterTreePreserveLinks(
                    target,
                    groups,
                    RBasic,
                    f => f.RosterBasic,
                    ref skippedTroops,
                    ref skippedRosters
                );

                imported += ApplyRosterTreePreserveLinks(
                    target,
                    groups,
                    RElite,
                    f => f.RosterElite,
                    ref skippedTroops,
                    ref skippedRosters
                );

                imported += ApplyRosterListPreserveLinks(
                    target,
                    groups,
                    RRetinues,
                    f => f.RosterRetinues,
                    ref skippedTroops,
                    ref skippedRosters
                );

                imported += ApplyRosterListPreserveLinks(
                    target,
                    groups,
                    RMilitia,
                    f => f.RosterMilitia,
                    ref skippedTroops,
                    ref skippedRosters
                );

                imported += ApplyRosterListPreserveLinks(
                    target,
                    groups,
                    RMercenary,
                    f => f.RosterMercenary,
                    ref skippedTroops,
                    ref skippedRosters
                );

                imported += ApplyRosterListPreserveLinks(
                    target,
                    groups,
                    RCaravan,
                    f => f.RosterCaravan,
                    ref skippedTroops,
                    ref skippedRosters
                );

                imported += ApplyRosterListPreserveLinks(
                    target,
                    groups,
                    RVillager,
                    f => f.RosterVillager,
                    ref skippedTroops,
                    ref skippedRosters
                );

                imported += ApplyRosterListPreserveLinks(
                    target,
                    groups,
                    RBandit,
                    f => f.RosterBandit,
                    ref skippedTroops,
                    ref skippedRosters
                );

                imported += ApplyRosterListPreserveLinks(
                    target,
                    groups,
                    RCivilian,
                    f => f.RosterCivilian,
                    ref skippedTroops,
                    ref skippedRosters
                );

                imported += ApplyRosterListPreserveLinks(
                    target,
                    groups,
                    RHeroes,
                    f => f.RosterHeroes,
                    ref skippedTroops,
                    ref skippedRosters
                );

                report.AppliedFactionPayload = false;
                report.ImportedTroops = imported;
                report.SkippedTroops = skippedTroops;
                report.SkippedRosters = skippedRosters;

                WCharacter.InvalidateTroopSourceCaches();

                Log.Debug(
                    $"Applied faction export to '{target.StringId}'. Imported={imported}, skippedTroops={skippedTroops}, skippedRosters={skippedRosters}."
                );
                return imported > 0;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "TryApplyFactionExport failed.");
                error = ex.Message ?? "unknown error.";
                return false;
            }
        }

        private static int ApplyRosterTreePreserveLinks(
            IBaseFaction target,
            Dictionary<string, List<CharacterExportEntry>> groups,
            string rosterKey,
            Func<IBaseFaction, List<WCharacter>> getRoots,
            ref int skippedTroops,
            ref int skippedRosters
        )
        {
            if (!groups.TryGetValue(rosterKey, out var src) || src == null || src.Count == 0)
                return 0; // roster absent from export => leave target alone

            var roots = getRoots?.Invoke(target);
            if (roots == null || roots.Count == 0)
            {
                skippedRosters++;
                skippedTroops += src.Count;
                return 0;
            }

            var dstTree = ExpandTreeFromRoots(roots);

            Log.Debug(
                $"Faction import roster '{rosterKey}': src={src.Count}, dstRoots={roots.Count}, dstTree={dstTree.Count}, firstRoot='{roots[0].StringId}', firstRootUpgrades={(roots[0].UpgradeTargets?.Count ?? 0)}."
            );

            if (dstTree.Count == 0)
            {
                skippedRosters++;
                skippedTroops += src.Count;
                return 0;
            }

            var count = Math.Min(src.Count, dstTree.Count);
            if (src.Count > count)
                skippedTroops += (src.Count - count);

            for (int i = 0; i < count; i++)
            {
                var entry = src[i];
                var dst = dstTree[i];

                if (entry == null || dst == null || string.IsNullOrWhiteSpace(entry.PayloadXml))
                {
                    skippedTroops++;
                    continue;
                }

                ApplyCharacterPayloadPreserveUpgradeTargets(dst, entry.PayloadXml);
            }

            return count;
        }

        private static int ApplyRosterListPreserveLinks(
            IBaseFaction target,
            Dictionary<string, List<CharacterExportEntry>> groups,
            string rosterKey,
            Func<IBaseFaction, List<WCharacter>> getRoster,
            ref int skippedTroops,
            ref int skippedRosters
        )
        {
            if (!groups.TryGetValue(rosterKey, out var src) || src == null || src.Count == 0)
                return 0; // roster absent from export => leave target alone

            var dst = getRoster?.Invoke(target);
            if (dst == null || dst.Count == 0)
            {
                skippedRosters++;
                skippedTroops += src.Count;
                return 0;
            }

            var count = Math.Min(src.Count, dst.Count);
            if (src.Count > count)
                skippedTroops += (src.Count - count);

            for (int i = 0; i < count; i++)
            {
                var entry = src[i];
                var troop = dst[i];

                if (entry == null || troop == null || string.IsNullOrWhiteSpace(entry.PayloadXml))
                {
                    skippedTroops++;
                    continue;
                }

                ApplyCharacterPayloadPreserveUpgradeTargets(troop, entry.PayloadXml);
            }

            return count;
        }

        private static List<WCharacter> ExpandTreeFromRoots(List<WCharacter> roots)
        {
            var ordered = new List<WCharacter>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            if (roots == null)
                return ordered;

            for (int i = 0; i < roots.Count; i++)
            {
                var r = roots[i];
                if (r == null || string.IsNullOrWhiteSpace(r.StringId))
                    continue;

                // Prefer wrapper Tree if available and populated.
                var tree = r.Tree;
                if (tree != null && tree.Count > 0)
                {
                    for (int j = 0; j < tree.Count; j++)
                    {
                        var t = tree[j];
                        if (t == null || string.IsNullOrWhiteSpace(t.StringId))
                            continue;

                        if (seen.Add(t.StringId))
                            ordered.Add(t);
                    }

                    continue;
                }

                // Fallback DFS via UpgradeTargets.
                TraverseTreeDfs(r, ordered, seen);
            }

            return ordered;
        }

        private static void TraverseTreeDfs(
            WCharacter node,
            List<WCharacter> ordered,
            HashSet<string> seen
        )
        {
            if (node == null || string.IsNullOrWhiteSpace(node.StringId))
                return;

            if (!seen.Add(node.StringId))
                return;

            ordered.Add(node);

            var next = node.UpgradeTargets;
            if (next == null || next.Count == 0)
                return;

            for (int i = 0; i < next.Count; i++)
                TraverseTreeDfs(next[i], ordered, seen);
        }

        private static void ApplyCharacterPayloadPreserveUpgradeTargets(
            WCharacter target,
            string payloadXml
        )
        {
            var existing = target.UpgradeTargets?.ToList() ?? [];

            var rewritten = RewriteCharacterPayload(payloadXml, keepUpgradeTargets: false);
            rewritten = ForceCharacterIdentity(rewritten, target.StringId);

            target.Deserialize(rewritten);
            target.UpgradeTargets = existing;
        }

        private static string RewriteCharacterPayload(string xml, bool keepUpgradeTargets)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return string.Empty;

            try
            {
                var el = XElement.Parse(xml, LoadOptions.None);

                if (!keepUpgradeTargets)
                {
                    var up = el.Elements()
                        .FirstOrDefault(x => x.Name.LocalName == "UpgradeTargetsAttribute");
                    up?.Remove();
                }

                return el.ToString(SaveOptions.DisableFormatting);
            }
            catch
            {
                return xml ?? string.Empty;
            }
        }

        private static string ForceCharacterIdentity(string xml, string forcedStringId)
        {
            if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(forcedStringId))
                return xml ?? string.Empty;

            try
            {
                var el = XElement.Parse(xml, LoadOptions.None);

                if (el.Attribute("stringId") != null)
                    el.SetAttributeValue("stringId", forcedStringId);

                return el.ToString(SaveOptions.DisableFormatting);
            }
            catch
            {
                return xml ?? string.Empty;
            }
        }
    }
}
