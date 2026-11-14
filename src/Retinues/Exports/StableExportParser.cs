using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Retinues.Migration.Legacy;

namespace Retinues.Exports
{
    internal sealed class StableExportSection
    {
        internal string Key;
        internal FactionSaveData Faction;
        internal Dictionary<string, TroopSaveData> Troops;
    }

    /// <summary>Reads stable's unified XML using the same data types as save migration.
    /// Parsing and Library previews never resolve or modify live campaign objects.</summary>
    internal static class StableExportParser
    {
        private static readonly XmlSerializer Serializer = new(typeof(FactionSaveData));

        internal static XDocument LoadDocument(string path)
        {
            using var stream = File.OpenRead(path);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 32 * 1024 * 1024,
            };
            // Bound nesting before XmlSerializer's recursive traversal.
            using (var reader = XmlReader.Create(stream, settings))
                while (reader.Read())
                    if (reader.Depth > 128)
                        throw new InvalidDataException("Export is nested too deeply.");
            stream.Position = 0;
            using var second = XmlReader.Create(stream, settings);
            return XDocument.Load(second);
        }

        internal static bool TryRead(string path, out List<StableExportSection> sections, out string error)
        {
            sections = null;
            error = null;
            try
            {
                sections = Read(LoadDocument(path).Root);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetBaseException().Message;
                return false;
            }
        }

        internal static List<StableExportSection> Read(XElement root)
        {
            if (root?.Name != "RetinuesTroops")
                throw new InvalidDataException("Expected a stable RetinuesTroops export.");

            var result = new List<StableExportSection>();
            Add(root.Element("Factions")?.Element("clanData"), "clan", result);
            Add(root.Element("Factions")?.Element("kingdomData"), "kingdom", result);
            int index = 0;
            foreach (var faction in root.Element("Cultures")?.Elements("FactionSaveData") ?? [])
                Add(faction, "culture:" + (++index), result);
            if (result.Count == 0)
                throw new InvalidDataException("The export contains no troop definitions.");
            return result;
        }

        private static void Add(XElement element, string key, List<StableExportSection> sections)
        {
            if (element == null)
                return;
            // Rename only the wrapper; all field names match the stable serializer.
            var wrapper = new XElement(element) { Name = "FactionSaveData" };
            using var reader = wrapper.CreateReader();
            var faction = (FactionSaveData)Serializer.Deserialize(reader);
            if (faction == null)
                return;
            var troops = Index(faction);
            if (troops.Count > 0)
                sections.Add(new StableExportSection { Key = key, Faction = faction, Troops = troops });
        }

        // Named slots remain named: sparse stable exports cannot be imported by list index.
        internal static IEnumerable<(string Role, TroopSaveData Troop)> Roots(FactionSaveData faction)
        {
            foreach (var field in typeof(FactionSaveData).GetFields())
            {
                if (field.Name == nameof(FactionSaveData.PrisonGuard))
                    continue; // Unused even in stable.
                if (field.GetValue(faction) is TroopSaveData troop)
                    yield return (field.Name, troop);
                else if (field.GetValue(faction) is List<TroopSaveData> list)
                    foreach (var item in list)
                        if (item != null)
                            yield return (field.Name, item);
            }
        }

        internal static Dictionary<string, TroopSaveData> Index(FactionSaveData faction)
        {
            var result = new Dictionary<string, TroopSaveData>(StringComparer.Ordinal);
            var active = new HashSet<string>(StringComparer.Ordinal);
            var fingerprints = new Dictionary<string, string>(StringComparer.Ordinal);
            int count = 0;
            void Visit(TroopSaveData troop, int depth)
            {
                if (troop == null)
                    return;
                if (++count > 10000 || depth > 48)
                    throw new InvalidDataException("Too many or too deeply nested troop definitions.");
                if (string.IsNullOrWhiteSpace(troop.StringId))
                    throw new InvalidDataException("A troop has no source ID.");
                if (!active.Add(troop.StringId))
                    throw new InvalidDataException("A troop tree contains a cycle: " + troop.StringId);
                if (troop.UpgradeTargets?.Count > 4)
                    throw new InvalidDataException("A troop has more than four upgrade targets.");

                // DAGs are serialized as repeated subtrees by stable. Accept identical records,
                // but reject ambiguous definitions instead of letting import order decide.
                string fingerprint = Fingerprint(troop);
                if (fingerprints.TryGetValue(troop.StringId, out var previous) && previous != fingerprint)
                    throw new InvalidDataException("Conflicting definitions for troop " + troop.StringId);
                fingerprints[troop.StringId] = fingerprint;
                result[troop.StringId] = troop;
                foreach (var child in troop.UpgradeTargets ?? [])
                {
                    if (child == null)
                        throw new InvalidDataException("A troop has an empty upgrade target.");
                    Visit(child, depth + 1);
                }
                Visit(troop.Captain, depth + 1);
                active.Remove(troop.StringId);
            }
            foreach (var root in Roots(faction))
                Visit(root.Troop, 0);
            return result;
        }

        private static readonly XmlSerializer TroopSerializer = new(typeof(TroopSaveData));

        private static string Fingerprint(TroopSaveData troop)
        {
            // Copy scalars/data containers but omit recursive edges when serializing the fingerprint.
            var copy = new TroopSaveData();
            foreach (var field in typeof(TroopSaveData).GetFields())
                if (field.Name != nameof(TroopSaveData.UpgradeTargets) && field.Name != nameof(TroopSaveData.Captain))
                    field.SetValue(copy, field.GetValue(troop));
            using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            TroopSerializer.Serialize(writer, copy);
            return writer + "\n" + string.Join(";", (troop.UpgradeTargets ?? []).Select(t => t?.StringId))
                + "\n" + troop.Captain?.StringId;
        }
    }
}
