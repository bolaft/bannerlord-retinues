using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Utilities;

namespace Retinues.Exports
{
    /// <summary>
    /// Applies Retinues faction exports to an existing in-game faction, preserving upgrade links.
    /// </summary>
    public static class FactionImporter
    {
        // A troop node can hold at most this many upgrade targets (matches the editor limit).
        private const int MaxTreeUpgradeTargets = 4;

        internal const string RHeroes = "heroes";
        internal const string RRetinues = "retinues";
        internal const string RElite = "elite";
        internal const string RBasic = "basic";
        internal const string RMercenary = "mercenary";
        internal const string RMilitia = "militia";
        internal const string RCaravan = "caravan";
        internal const string RVillager = "villager";
        internal const string RBandit = "bandit";
        internal const string RCivilian = "civilian";
        internal const string RAll = "all";

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

                imported += ApplyRosterTreeMirror(
                    target,
                    groups,
                    RBasic,
                    f => f.RootBasic,
                    ref skippedTroops,
                    ref skippedRosters
                );

                imported += ApplyRosterTreeMirror(
                    target,
                    groups,
                    RElite,
                    f => f.RootElite,
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
                Log.Exception(ex, "FactionImportService.TryApplyFactionExport failed.");
                error = ex.Message ?? "unknown error.";
                return false;
            }
        }

        /// <summary>
        /// Imports a tree roster (basic/elite) by mirroring the exported tree's *structure* onto the
        /// destination: existing destination nodes are re-skinned, and missing nodes are created by
        /// cloning so the whole source tree is reproduced — not just the nodes that already existed.
        /// </summary>
        private static int ApplyRosterTreeMirror(
            IBaseFaction target,
            Dictionary<string, List<CharacterExportEntry>> groups,
            string rosterKey,
            Func<IBaseFaction, WCharacter> getRoot,
            ref int skippedTroops,
            ref int skippedRosters
        )
        {
            if (!groups.TryGetValue(rosterKey, out var src) || src == null || src.Count == 0)
                return 0; // roster absent from export => leave target alone

            var dstRoot = getRoot?.Invoke(target);
            if (dstRoot?.Base == null)
            {
                skippedRosters++;
                skippedTroops += src.Count;
                return 0;
            }

            // Index the exported entries by their (source) string id.
            var srcById = new Dictionary<string, CharacterExportEntry>(StringComparer.Ordinal);
            foreach (var e in src)
            {
                var id = e?.SourceId;
                if (!string.IsNullOrWhiteSpace(id) && !srcById.ContainsKey(id))
                    srcById[id] = e;
            }

            // Reconstruct the source parent->children structure from each payload's upgrade targets.
            var childrenById = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var referenced = new HashSet<string>(StringComparer.Ordinal);
            foreach (var e in src)
            {
                var id = e?.SourceId;
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var kids = ParseUpgradeTargetChildIds(e.PayloadXml);
                childrenById[id] = kids;
                for (int i = 0; i < kids.Count; i++)
                    referenced.Add(kids[i]);
            }

            // The root is the single entry no one upgrades into (fallback to the first entry).
            CharacterExportEntry srcRoot = null;
            foreach (var e in src)
            {
                var id = e?.SourceId;
                if (!string.IsNullOrWhiteSpace(id) && !referenced.Contains(id))
                {
                    srcRoot = e;
                    break;
                }
            }
            srcRoot ??= src[0];

            Log.Debug(
                $"Faction import roster '{rosterKey}': srcNodes={src.Count}, dstRoot='{dstRoot.StringId}', srcRoot='{srcRoot?.SourceId}'."
            );

            int imported = 0;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            MirrorNode(srcRoot, dstRoot, srcById, childrenById, visited, ref imported);

            // Anything we could not place (extra roots, over-the-cap children, exhausted stub pool).
            foreach (var e in src)
            {
                var id = e?.SourceId;
                if (!string.IsNullOrWhiteSpace(id) && !visited.Contains(id))
                    skippedTroops++;
            }

            return imported;
        }

        /// <summary>
        /// Recursively re-skins <paramref name="dstNode"/> from the source entry and reproduces its
        /// children, cloning new destination troops where the destination is missing nodes.
        /// </summary>
        private static void MirrorNode(
            CharacterExportEntry srcEntry,
            WCharacter dstNode,
            Dictionary<string, CharacterExportEntry> srcById,
            Dictionary<string, List<string>> childrenById,
            HashSet<string> visited,
            ref int imported
        )
        {
            if (srcEntry?.SourceId == null || dstNode?.Base == null)
                return;

            if (!visited.Add(srcEntry.SourceId))
                return; // guard against malformed/cyclic exports

            if (!string.IsNullOrWhiteSpace(srcEntry.PayloadXml))
            {
                ApplyCharacterPayloadPreserveUpgradeTargets(dstNode, srcEntry.PayloadXml);
                imported++;
            }

            if (
                !childrenById.TryGetValue(srcEntry.SourceId, out var childIds)
                || childIds == null
                || childIds.Count == 0
            )
                return;

            for (int c = 0; c < childIds.Count && c < MaxTreeUpgradeTargets; c++)
            {
                if (!srcById.TryGetValue(childIds[c], out var srcChild) || srcChild == null)
                    continue;

                // Re-read existing children each iteration (AddUpgradeTarget mutates the list).
                var existing = dstNode.UpgradeTargets;
                WCharacter dstChild;

                if (existing != null && c < existing.Count && existing[c]?.Base != null)
                {
                    dstChild = existing[c];
                }
                else
                {
                    var clone = CharacterCloner.Clone(dstNode, equipments: false);
                    if (clone == null)
                        continue; // stub pool exhausted -> leave the rest to the skipped tally

                    clone.HiddenInEncyclopedia = false;
                    dstNode.AddUpgradeTarget(clone);
                    dstChild = clone;
                }

                MirrorNode(srcChild, dstChild, srcById, childrenById, visited, ref imported);
            }
        }

        /// <summary>
        /// Extracts the child (upgrade target) string ids from a serialized character payload.
        /// </summary>
        private static List<string> ParseUpgradeTargetChildIds(string payloadXml)
        {
            var ids = new List<string>();
            if (string.IsNullOrWhiteSpace(payloadXml))
                return ids;

            try
            {
                var el = System.Xml.Linq.XElement.Parse(
                    payloadXml,
                    System.Xml.Linq.LoadOptions.None
                );

                var up = el.Elements()
                    .FirstOrDefault(x => x.Name.LocalName == "UpgradeTargetsAttribute");
                if (up == null)
                    return ids;

                foreach (var item in up.Elements())
                {
                    if (item.Name.LocalName != "Item")
                        continue;

                    var id = (item.Value ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(id))
                        ids.Add(id);
                }
            }
            catch
            {
                // Malformed payload -> treat as a leaf.
            }

            return ids;
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
                var el = System.Xml.Linq.XElement.Parse(xml, System.Xml.Linq.LoadOptions.None);

                if (!keepUpgradeTargets)
                {
                    var up = el.Elements()
                        .FirstOrDefault(x => x.Name.LocalName == "UpgradeTargetsAttribute");
                    up?.Remove();
                }

                return el.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
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
                var el = System.Xml.Linq.XElement.Parse(xml, System.Xml.Linq.LoadOptions.None);

                if (el.Attribute("stringId") != null)
                    el.SetAttributeValue("stringId", forcedStringId);

                return el.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
            }
            catch
            {
                return xml ?? string.Empty;
            }
        }
    }
}
