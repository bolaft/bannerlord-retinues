using System.Collections.Generic;
using System.IO;
using System.Linq;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Model.Exports;
using Retinues.UI.Services;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers.Library
{
    public partial class LibraryController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Import targets                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the current mode provides valid import targets.
        /// </summary>
        private static bool CanResolveImportContext(MLibrary.Item item)
        {
            if (item == null)
                return false;

            if (item.Kind == MLibraryKind.Character)
            {
                var faction = EditorState.Instance.Faction;
                if (faction == null)
                    return false;

                return GetTroopImportTargets(faction).Count > 0;
            }

            if (item.Kind == MLibraryKind.Faction)
            {
                // Player mode: no kingdom => auto-select player clan.
                if (EditorState.Instance.Mode == EditorMode.Player && Player.Kingdom == null)
                    return Player.Clan != null;

                return GetFactionImportTargets().Count > 0;
            }

            return false;
        }

        /// <summary>
        /// Builds a localized reason for why import targets cannot be resolved.
        /// </summary>
        private static TextObject BuildCantResolveImportContextReason(MLibrary.Item item)
        {
            if (item == null)
                return L.T("library_import_no_selection", "No export selected.");

            if (item.Kind == MLibraryKind.Character)
            {
                if (EditorState.Instance.Faction == null)
                    return L.T(
                        "library_import_no_faction",
                        "No faction is selected in the editor."
                    );

                return L.T(
                    "library_import_no_troop_targets",
                    "No troops are available to be replaced in the current faction."
                );
            }

            if (item.Kind == MLibraryKind.Faction)
            {
                return L.T(
                    "library_import_no_faction_targets",
                    "No factions are available to be overridden in the current editor mode."
                );
            }

            return L.T("library_import_failed_invalid", "The export could not be imported.");
        }

        /// <summary>
        /// Returns replaceable troops for the current faction.
        /// </summary>
        private static List<WCharacter> GetTroopImportTargets(IBaseFaction faction)
        {
            if (faction == null)
                return [];

            var list = faction.Troops?.ToList() ?? [];
            if (list.Count == 0)
                return [];

            // Avoid heroes by default.
            list.RemoveAll(t => t == null || t.IsHero);

            // Stable ordering.
            list =
            [
                .. list.OrderBy(t => t.IsElite ? 0 : 1).ThenBy(t => t.Tier).ThenBy(t => t.Name),
            ];

            return list;
        }

        /// <summary>
        /// Returns selectable import targets for faction override.
        /// </summary>
        private static List<IBaseFaction> GetFactionImportTargets()
        {
            var targets = new List<IBaseFaction>();

            if (EditorState.Instance.Mode == EditorMode.Player)
            {
                // In player mode, allow player kingdom + any clan.
                if (Player.Kingdom != null)
                    targets.Add(Player.Kingdom);

                foreach (var clan in WClan.All)
                {
                    if (clan == null)
                        continue;

                    targets.Add(clan);
                }

                // Ensure player clan is present.
                if (Player.Clan != null && !targets.Any(t => ReferenceEquals(t, Player.Clan)))
                    targets.Add(Player.Clan);

                // Unique by StringId.
                return
                [
                    .. targets
                        .Where(t => t != null && !string.IsNullOrWhiteSpace(t.StringId))
                        .GroupBy(t => t.StringId)
                        .Select(g => g.First())
                        .OrderBy(t => t.Name),
                ];
            }

            // Universal: include cultures, kingdoms, clans.
            foreach (var culture in WCulture.All)
                if (culture != null)
                    targets.Add(culture);

            return
            [
                .. targets
                    .Where(t => t != null && !string.IsNullOrWhiteSpace(t.StringId))
                    .GroupBy(t => t.StringId)
                    .Select(g => g.First())
                    .OrderBy(t => t.Name),
            ];
        }

        /// <summary>
        /// Returns a faction image identifier for inquiry UI.
        /// </summary>
        private static ImageIdentifier GetFactionImageIdentifier(IBaseFaction f)
        {
            // All of our faction wrappers expose ImageIdentifier even if IBaseFaction doesn't.
            if (f is WCulture culture)
                return culture.ImageIdentifier;

            if (f is WClan clan)
                return clan.ImageIdentifier;

            if (f is WKingdom kingdom)
                return kingdom.ImageIdentifier;

            // Fallback: no image.
            return null;
        }

        /// <summary>
        /// Unwraps a MultiSelect selection entry into an IBaseFaction.
        /// </summary>
        private static bool TryUnwrapFactionSelection(object o, out IBaseFaction faction)
        {
            faction = null;

            if (o is IBaseFaction f)
            {
                faction = f;
                return true;
            }

            if (o is InquiryElement ie && ie.Identifier is IBaseFaction f2)
            {
                faction = f2;
                return true;
            }

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Shared helpers                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if the export file exists on disk.
        /// </summary>
        private static bool HasExistingFile(MLibrary.Item item)
        {
            var path = item?.FilePath ?? string.Empty;
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }
    }
}
