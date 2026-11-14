using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Migration;
using Retinues.Migration.Legacy;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Exports
{
    internal static class StableFactionImporter
    {
        internal static bool TryApply(IBaseFaction target, StableExportSection section,
            out ImportReport report, out string error)
        {
            report = new ImportReport();
            error = null;
            try
            {
                if (Safety.Uninstall.UninstallBehavior.IsPrepared)
                    throw new InvalidOperationException("Cannot import after preparing safe uninstall.");
                // Validate the whole section before any live troop is changed or allocated.
                var sources = StableExportParser.Index(section.Faction);
                foreach (var source in sources.Values)
                    ValidateDependencies(source);

                var placed = new Dictionary<string, WCharacter>(StringComparer.Ordinal);
                var claimed = new HashSet<string>(StringComparer.Ordinal);
                var roots = new List<TroopSaveData>();
                var retinues = target.RosterRetinues ?? [];
                foreach (var slot in StableExportParser.Roots(section.Faction))
                {
                    var troop = slot.Troop;
                    if (placed.ContainsKey(troop.StringId))
                        continue;
                    var destination = ResolveSlot(target, slot.Role, troop);
                    if (slot.Role == nameof(FactionSaveData.RetinueElite) || slot.Role == nameof(FactionSaveData.RetinueBasic))
                    {
                        int index = slot.Role == nameof(FactionSaveData.RetinueBasic) && section.Faction.RetinueElite != null ? 1 : 0;
                        destination = retinues.ElementAtOrDefault(index);
                        // V2 normally has one retinue; keep both stable retinues when present.
                        if (destination == null && retinues.Count > 0 && (target is WClan || target is WKingdom))
                        {
                            destination = CharacterCloner.Clone(retinues[0]);
                            if (destination != null)
                            {
                                if (target is WClan clan) clan.AddRetinue(destination);
                                if (target is WKingdom kingdom) kingdom.AddRetinue(destination);
                                retinues.Add(destination);
                            }
                        }
                    }
                    if (destination?.Base == null || !claimed.Add(destination.StringId))
                    {
                        report.SkippedRosters++;
                        continue;
                    }
                    placed.Add(troop.StringId, destination);
                    roots.Add(troop);
                }

                // Reserve named roots first so a child never takes another roster's destination.
                var visited = new HashSet<string>(StringComparer.Ordinal);
                foreach (var root in roots)
                    PlaceTree(root, placed[root.StringId], placed, claimed, visited);

                try
                {
                    foreach (var pair in placed)
                    {
                        var source = sources[pair.Key];
                        var destination = pair.Value;
                        LegacyCharacterConverter.Apply(destination, source);
                        // Native edits also carry their original identity when imported onto a stub.
                        if (destination.IsCustom && !string.IsNullOrWhiteSpace(source.VanillaStringId))
                            destination.SourceStringId = source.VanillaStringId;
                        report.ImportedTroops++;
                    }
                }
                finally
                {
                    // Link only after definitions are applied. Restore existing captain
                    // relationships even if an unrelated record failed during conversion.
                    foreach (var pair in placed)
                        LegacyCharacterConverter.LinkCaptain(pair.Value, sources[pair.Key],
                            id => placed.TryGetValue(id, out var captain) ? captain : null);
                }
                report.SkippedTroops = sources.Count - placed.Count;
                WCharacter.InvalidateTroopSourceCaches();
                Log.Info($"[Stable import] Section '{section.Key}' -> '{target.StringId}': "
                    + $"{report.ImportedTroops} imported, {report.SkippedTroops} skipped, {report.SkippedRosters} unavailable roots.");
                foreach (var id in sources.Keys.Where(id => !placed.ContainsKey(id)))
                    Log.Warning($"[Stable import] No destination for '{id}' in '{target.StringId}'; left unchanged.");
                if (report.ImportedTroops == 0)
                    error = "No matching troops are available in the selected faction. Check its troop settings or select the matching culture in the Global Editor.";
                return report.ImportedTroops > 0;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Log.Exception(ex, "Stable faction import failed.");
                return false;
            }
        }

        internal static WCharacter ResolveSlot(IBaseFaction target, string role, TroopSaveData source)
        {
            return role switch
            {
                nameof(FactionSaveData.RootBasic) => target.RootBasic,
                nameof(FactionSaveData.RootElite) => target.RootElite,
                nameof(FactionSaveData.MilitiaMelee) => target.MeleeMilitiaTroop,
                nameof(FactionSaveData.MilitiaMeleeElite) => target.MeleeEliteMilitiaTroop,
                nameof(FactionSaveData.MilitiaRanged) => target.RangedMilitiaTroop,
                nameof(FactionSaveData.MilitiaRangedElite) => target.RangedEliteMilitiaTroop,
                nameof(FactionSaveData.CaravanGuard) => target.CaravanGuard,
                nameof(FactionSaveData.VeteranCaravanGuard) => target.VeteranCaravanGuard,
                nameof(FactionSaveData.CaravanMaster) => target.CaravanMaster,
                nameof(FactionSaveData.Villager) => target.Villager,
                nameof(FactionSaveData.Civilians) => Match(source, target.RosterCivilian),
                nameof(FactionSaveData.Bandits) => Match(source, target.RosterBandit),
                nameof(FactionSaveData.Heroes) => Match(source, target.RosterHeroes),
                nameof(FactionSaveData.Mercenaries) => Match(source, target.RosterMercenary),
                nameof(FactionSaveData.Extras) => Match(source, target.Troops),
                _ => null,
            };
        }

        // Restrict identity matching to the chosen faction. Never resolve an export stub ID
        // globally: it can identify a completely different active troop in this campaign.
        private static WCharacter Match(TroopSaveData source, IEnumerable<WCharacter> candidates)
        {
            var list = candidates?.Where(t => t?.Base != null).Distinct().ToList() ?? [];
            var native = list.FirstOrDefault(t => !t.IsCustom && t.StringId == source.StringId);
            if (native != null) return native;
            var origins = list.Where(t => !string.IsNullOrEmpty(source.VanillaStringId)
                && (t.StringId == source.VanillaStringId || t.SourceStringId == source.VanillaStringId)).ToList();
            // Stub numbers are campaign-local; an ambiguous template match must be skipped.
            return origins.Count == 1 ? origins[0] : null;
        }

        internal static void PlaceTree(TroopSaveData source, WCharacter destination,
            Dictionary<string, WCharacter> placed, HashSet<string> claimed, HashSet<string> visited)
        {
            if (!visited.Add(source.StringId)) return;
            var links = destination.UpgradeTargets?.ToList() ?? [];
            for (int i = 0; i < (source.UpgradeTargets?.Count ?? 0); i++)
            {
                var child = source.UpgradeTargets[i];
                if (!placed.TryGetValue(child.StringId, out var next))
                {
                    next = links.ElementAtOrDefault(i);
                    if (next?.Base == null || claimed.Contains(next.StringId))
                        next = CharacterCloner.Clone(destination);
                    if (next == null) continue;
                    placed[child.StringId] = next;
                    claimed.Add(next.StringId);
                }
                if (i < links.Count) links[i] = next;
                else links.Add(next);
                PlaceTree(child, next, placed, claimed, visited);
            }
            destination.UpgradeTargets = links;
            if (source.Captain != null)
            {
                var captain = source.Captain;
                if (!placed.TryGetValue(captain.StringId, out var next))
                {
                    next = destination.Captain;
                    if (next?.Base == null || claimed.Contains(next.StringId))
                        next = CharacterCloner.Clone(destination);
                    if (next == null) return;
                    placed[captain.StringId] = next;
                    claimed.Add(next.StringId);
                }
                PlaceTree(captain, next, placed, claimed, visited);
            }
        }

        private static void ValidateDependencies(TroopSaveData source)
        {
            var objects = MBObjectManager.Instance;
            if (source.Race >= RaceHelper.GetRaceCount())
                throw new InvalidDataException($"'{source.Name}': missing race '{source.Race}'. Load the mods used by the export first.");
            if (!string.IsNullOrEmpty(source.CultureId) && objects.GetObject<CultureObject>(source.CultureId) == null)
                throw new InvalidDataException($"'{source.Name}': missing culture '{source.CultureId}'. Load the mods used by the export first.");
            foreach (var code in source.EquipmentData?.Codes ?? [])
                ValidateEquipmentCode(code, id => objects.GetObject<ItemObject>(id) != null,
                    id => objects.GetObject<ItemModifier>(id) != null);
        }

        internal static void ValidateEquipmentCode(string code, Func<string, bool> hasItem, Func<string, bool> hasModifier)
        {
            if (string.IsNullOrEmpty(code)) return;
            foreach (var slot in code.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = slot.Split('-');
                if (parts.Length != 3 || !int.TryParse(parts[0], out int index) || index < 0
                    || index >= (int)EquipmentIndex.NumEquipmentSetSlots)
                    throw new InvalidDataException("Invalid equipment code in stable export.");
                if (parts[1] != "@null" && !hasItem(parts[1]))
                    throw new InvalidDataException($"Missing item '{parts[1]}'. Load the mods used by the export first.");
                if (parts[2] != "@null" && !hasModifier(parts[2]))
                    throw new InvalidDataException($"Missing item modifier '{parts[2]}'. Load the mods used by the export first.");
            }
        }
    }
}
