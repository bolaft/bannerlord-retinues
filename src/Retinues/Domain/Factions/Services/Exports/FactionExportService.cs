// File: src/Retinues/Domain/Factions/Services/Exports/FactionExportService.cs

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Retinues.Domain.Characters.Services.Exports;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;

namespace Retinues.Domain.Factions.Services.Exports
{
    /// <summary>
    /// Builds Retinues exports for factions.
    /// </summary>
    public static class FactionExportService
    {
        public const string RHeroes = "heroes";
        public const string RRetinues = "retinues";
        public const string RElite = "elite";
        public const string RBasic = "basic";
        public const string RMercenary = "mercenary";
        public const string RMilitia = "militia";
        public const string RCaravan = "caravan";
        public const string RVillager = "villager";
        public const string RBandit = "bandit";
        public const string RCivilian = "civilian";
        public const string RAll = "all";

        public static bool TryBuildExport(IBaseFaction faction, out XDocument doc, out string error)
        {
            doc = null;
            error = null;

            if (faction == null)
            {
                error = "faction is null.";
                return false;
            }

            var root = RetinuesExportXml.BuildRoot(kind: "faction", sourceId: faction.StringId);

            RetinuesExportXml.AddSerialized(root, null, faction.SerializeAll());

            var troops = CollectFactionTroopsWithRosterKeys(faction);

            for (int i = 0; i < troops.Count; i++)
            {
                var (t, rosterKey) = troops[i];

                if (t == null || string.IsNullOrWhiteSpace(t.StringId))
                    continue;

                if (t.IsHero)
                    continue;

                var added = RetinuesExportXml.AddSerialized(root, t.UniqueId, t.SerializeAll());

                if (added != null && !string.IsNullOrWhiteSpace(rosterKey))
                    added.SetAttributeValue("r", rosterKey);
            }

            doc = RetinuesExportXml.ToDocument(root);
            return true;
        }

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
