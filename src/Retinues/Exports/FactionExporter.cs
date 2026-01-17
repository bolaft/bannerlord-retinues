using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;

namespace Retinues.Exports
{
    /// <summary>
    /// Builds Retinues exports for factions.
    /// </summary>
    public static class FactionExporter
    {
        public const string RRetinues = "retinues";
        public const string RElite = "elite";
        public const string RBasic = "basic";
        public const string RMercenary = "mercenary";
        public const string RMilitia = "militia";
        public const string RCaravan = "caravan";
        public const string RVillager = "villager";
        public const string RBandit = "bandit";
        public const string RCivilian = "civilian";

        /// <summary>
        /// Attempts to build an export XDocument for the given faction.
        /// </summary>
        public static bool TryBuildExport(IBaseFaction faction, out XDocument doc, out string error)
        {
            doc = null;
            error = null;

            if (faction == null)
            {
                error = "Faction is null.";
                return false;
            }

            var root = ExportXML.BuildRoot(kind: "faction", sourceId: faction.StringId);

            ExportXML.AddSerialized(root, null, faction.SerializeAll());

            var troops = CollectFactionTroopsWithRosterKeys(faction);

            for (int i = 0; i < troops.Count; i++)
            {
                var (t, rosterKey) = troops[i];

                if (t == null || string.IsNullOrWhiteSpace(t.StringId))
                    continue;

                if (t.IsHero)
                    continue;

                var added = ExportXML.AddSerialized(root, t.UniqueId, t.SerializeAll());

                if (added != null && !string.IsNullOrWhiteSpace(rosterKey))
                    added.SetAttributeValue("r", rosterKey);
            }

            doc = ExportXML.ToDocument(root);
            return true;
        }

        /// <summary>
        /// Collects troops from the faction rosters along with their roster keys, preserving order and uniqueness.
        /// </summary>
        private static List<(
            WCharacter troop,
            string rosterKey
        )> CollectFactionTroopsWithRosterKeys(IBaseFaction f)
        {
            var ordered = new List<(WCharacter, string)>();

            AddMany(ordered, f.RosterRetinues, RRetinues);
            AddMany(ordered, f.RosterElite, RElite);
            AddMany(ordered, f.RosterBasic, RBasic);
            AddMany(ordered, f.RosterMercenary, RMercenary);
            AddMany(ordered, f.RosterMilitia, RMilitia);
            AddMany(ordered, f.RosterCaravan, RCaravan);
            AddMany(ordered, f.RosterVillager, RVillager);
            AddMany(ordered, f.RosterBandit, RBandit);
            AddMany(ordered, f.RosterCivilian, RCivilian);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unique = new List<(WCharacter, string)>();

            for (int i = 0; i < ordered.Count; i++)
            {
                var (t, k) = ordered[i];
                var id = t?.StringId;

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (!seen.Add(id))
                    continue;

                unique.Add((t, k));
            }

            return unique;
        }

        /// <summary>
        /// Adds many troops from a roster into the provided list with the given roster key.
        /// </summary>
        private static void AddMany(
            List<(WCharacter troop, string rosterKey)> list,
            List<WCharacter> troops,
            string rosterKey
        )
        {
            if (list == null || troops == null || troops.Count == 0)
                return;

            for (int i = 0; i < troops.Count; i++)
            {
                var t = troops[i];
                if (t == null)
                    continue;

                list.Add((t, rosterKey));
            }
        }
    }
}
