using System.Collections.Generic;

namespace Retinues.Exports
{
    /// <summary>
    /// Represents a single character export entry with source id, roster key and payload.
    /// </summary>
    public sealed class CharacterExportEntry
    {
        public string SourceId { get; set; }
        public string RosterKey { get; set; }
        public string PayloadXml { get; set; }
    }

    /// <summary>
    /// Contains exported faction data and its included character exports.
    /// </summary>
    public sealed class FactionExportData
    {
        public string SourceFactionId { get; set; }
        public string FactionPayloadXml { get; set; }
        public List<CharacterExportEntry> Troops { get; set; } = [];
    }

    /// <summary>
    /// Report summarizing results of an import operation.
    /// </summary>
    public sealed class ImportReport
    {
        public bool AppliedFactionPayload { get; set; }
        public int ImportedTroops { get; set; }
        public int SkippedTroops { get; set; }
        public int SkippedRosters { get; set; }
    }
}
