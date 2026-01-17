using System.Collections.Generic;

namespace Retinues.Domain.Exports.Services.Import
{
    public sealed class CharacterExportEntry
    {
        public string SourceId { get; set; }
        public string RosterKey { get; set; }
        public string PayloadXml { get; set; }
    }

    public sealed class FactionExportData
    {
        public string SourceFactionId { get; set; }
        public string FactionPayloadXml { get; set; }
        public List<CharacterExportEntry> Troops { get; set; } = [];
    }

    public sealed class ImportReport
    {
        public bool AppliedFactionPayload { get; set; }
        public int ImportedTroops { get; set; }
        public int SkippedTroops { get; set; }
        public int SkippedRosters { get; set; }
    }
}
