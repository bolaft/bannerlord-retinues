using System.Collections.Generic;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Runtime;

namespace Retinues.Framework.Model.Exports
{
    [SafeClass(IncludeDerived = true)]
    public static partial class MImportExport
    {
        internal const string ExportFolderName = "Exports";

        internal const string RootName = "Retinues";
        internal const string RootVersion = "1";

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

        internal static IBaseFaction ResolveFaction(string stringId)
        {
            var clan = WClan.Get(stringId);
            if (clan != null)
                return clan;

            var kingdom = WKingdom.Get(stringId);
            if (kingdom != null)
                return kingdom;

            var culture = WCulture.Get(stringId);
            if (culture != null)
                return culture;

            return null;
        }
    }
}
