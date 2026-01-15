using System;
using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions;
using Retinues.Editor.Events;
using Retinues.Framework.Model.Exports;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Library
{
    public partial class LibraryController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Import                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Entry point for the Import action.
        /// </summary>
        private static void ExecuteImport(MLibrary.Item item)
        {
            if (item == null)
                return;

            ApplyImport(item);
        }

        /// <summary>
        /// Starts the import flow based on export kind.
        /// </summary>
        private static void ApplyImport(MLibrary.Item item)
        {
            try
            {
                if (item == null)
                    return;

                var path = item.FilePath ?? string.Empty;

                if (item.Kind == MLibraryKind.Character)
                {
                    ApplyTroopImportWithSelection(path);
                    return;
                }

                if (item.Kind == MLibraryKind.Faction)
                {
                    ApplyFactionImportWithSelection(path);
                    return;
                }

                Inquiries.Popup(
                    title: L.T("library_import_failed_title", "Import Failed"),
                    description: L.T(
                        "library_import_failed_invalid",
                        "The export could not be imported."
                    )
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.ApplyImport failed.");
                Inquiries.Popup(
                    title: L.T("library_import_failed_title", "Import Failed"),
                    description: L.T(
                        "library_import_failed_exception",
                        "The file could not be imported."
                    )
                );
            }
        }

        /// <summary>
        /// Parses a troop export and lets the user choose a troop to replace.
        /// </summary>
        private static void ApplyTroopImportWithSelection(string path)
        {
            var faction = EditorState.Instance.Faction;
            if (faction == null)
            {
                Inquiries.Popup(
                    title: L.T("library_import_failed_title", "Import Failed"),
                    description: L.T(
                        "library_import_no_faction",
                        "No faction is selected in the editor."
                    )
                );
                return;
            }

            if (!MImportExport.TryParseCharacterExport(path, out var entry, out var err))
            {
                Inquiries.Popup(
                    title: L.T("library_import_failed_title", "Import Failed"),
                    description: L.T(
                        "library_import_failed_invalid",
                        "The export could not be imported."
                    )
                );
                if (!string.IsNullOrWhiteSpace(err))
                    Log.Warn($"Import troop parse failed: {err}");
                return;
            }

            var candidates = GetTroopImportTargets(faction);
            if (candidates.Count == 0)
            {
                Inquiries.Popup(
                    title: L.T("library_import_failed_title", "Import Failed"),
                    description: L.T(
                        "library_import_no_troop_targets",
                        "No troops are available to be replaced in the current faction."
                    )
                );
                return;
            }

            var elements = new List<InquiryElement>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                var t = candidates[i];
                if (t == null)
                    continue;

                elements.Add(
                    new InquiryElement(
                        identifier: t,
                        title: BuildTroopTitle(t),
                        imageIdentifier: t.GetImageIdentifier()
                    )
                );
            }

            Inquiries.SelectPopup(
                title: L.T("library_import_replace_troop_title", "Replace Troop"),
                description: L.T(
                        "library_import_replace_troop_desc",
                        "Select the troop to replace in {FACTION}."
                    )
                    .SetTextVariable("FACTION", faction.Name),
                elements: elements,
                onSelect: el =>
                {
                    var target = el?.Identifier as WCharacter;
                    if (target == null)
                        return;

                    ConfirmAndApplyTroopImport(entry, faction, target);
                }
            );
        }

        /// <summary>
        /// Parses a faction export and lets the user choose one or more factions to override.
        /// </summary>
        private static void ApplyFactionImportWithSelection(string path)
        {
            if (!MImportExport.TryParseFactionExport(path, out var data, out var err))
            {
                Inquiries.Popup(
                    title: L.T("library_import_failed_title", "Import Failed"),
                    description: L.T(
                        "library_import_failed_invalid",
                        "The export could not be imported."
                    )
                );
                if (!string.IsNullOrWhiteSpace(err))
                    Log.Warn($"Import faction parse failed: {err}");
                return;
            }

            var targets = GetFactionImportTargets();
            if (targets.Count == 0)
            {
                Inquiries.Popup(
                    title: L.T("library_import_failed_title", "Import Failed"),
                    description: L.T(
                        "library_import_no_faction_targets",
                        "No factions are available to be overridden in the current editor mode."
                    )
                );
                return;
            }

            // Player mode special case: no kingdom => auto-select player clan.
            if (EditorState.Instance.Mode == EditorMode.Player && Player.Kingdom == null)
            {
                var playerClan = Player.Clan;
                if (playerClan != null)
                    ConfirmAndApplyFactionImport(data, playerClan);
                else
                    Inquiries.Popup(
                        title: L.T("library_import_failed_title", "Import Failed"),
                        description: L.T(
                            "library_import_no_player_clan",
                            "Could not resolve the player clan."
                        )
                    );

                return;
            }

            var elements = new List<InquiryElement>(targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                var f = targets[i];
                if (f == null)
                    continue;

                elements.Add(
                    new InquiryElement(
                        identifier: f,
                        title: f.Name,
                        imageIdentifier: GetFactionImageIdentifier(f)
                    )
                );
            }

            Inquiries.SelectPopup(
                title: L.T("library_import_override_faction_title", "Override Faction"),
                description: L.T(
                    "library_import_override_faction_desc",
                    "Select the faction to override with this import."
                ),
                elements: elements,
                onSelect: el =>
                {
                    if (el?.Identifier is not IBaseFaction target)
                        return;

                    ConfirmAndApplyFactionImport(data, target);
                }
            );
        }

        /// <summary>
        /// Shows final confirmation for a troop import and applies it.
        /// </summary>
        private static void ConfirmAndApplyTroopImport(
            MImportExport.CharacterExportEntry entry,
            IBaseFaction faction,
            WCharacter target
        )
        {
            var desc = L.T(
                    "library_import_confirm_desc_replace_troop",
                    "{TARGET} in {FACTION} will be replaced with the imported troop.\n\nContinue?"
                )
                .SetTextVariable("TARGET", target?.Name ?? "Unknown")
                .SetTextVariable("FACTION", faction?.Name ?? "Unknown");

            Inquiries.Popup(
                title: L.T("library_import_confirm_title", "Import"),
                description: desc,
                onConfirm: () =>
                {
                    if (!MImportExport.TryApplyCharacterExport(target, entry, out var applyErr))
                    {
                        Inquiries.Popup(
                            title: L.T("library_import_failed_title", "Import Failed"),
                            description: L.T(
                                "library_import_failed_invalid",
                                "The export could not be imported."
                            )
                        );
                        if (!string.IsNullOrWhiteSpace(applyErr))
                            Log.Warn($"Import troop apply failed: {applyErr}");
                        return;
                    }

                    RefreshUiAfterImport();
                    LaunchEditorAfterImport(faction, target);
                }
            );
        }

        /// <summary>
        /// Shows final confirmation for a faction import and applies it.
        /// </summary>
        private static void ConfirmAndApplyFactionImport(
            MImportExport.FactionExportData data,
            IBaseFaction target
        )
        {
            if (data == null || target == null)
                return;

            var desc = L.T(
                    "library_import_confirm_desc_faction_final_single",
                    "Override faction:\n- {NAME}\n\nBasic and Elite troop trees will be overwritten.\nRosters missing from the export will be left unchanged.\n\nContinue?"
                )
                .SetTextVariable("NAME", target.Name);

            Inquiries.Popup(
                title: L.T("library_import_confirm_title", "Import"),
                description: desc,
                onConfirm: () => ApplyFactionExportToTarget(data, target)
            );
        }

        /// <summary>
        /// Applies a faction export to the selected target.
        /// </summary>
        private static void ApplyFactionExportToTarget(
            MImportExport.FactionExportData data,
            IBaseFaction target
        )
        {
            if (data == null || target == null)
                return;

            if (!MImportExport.TryApplyFactionExport(target, data, out var report, out var err))
            {
                Log.Warn(
                    $"Faction import apply failed for '{target.StringId}': {err ?? "unknown error"}"
                );

                Inquiries.Popup(
                    title: L.T("library_import_failed_title", "Import Failed"),
                    description: L.T(
                        "library_import_failed_invalid",
                        "The export could not be imported."
                    )
                );

                return;
            }

            Log.Debug(
                $"Faction import applied to '{target.StringId}': troops imported={report.ImportedTroops}, troops skipped={report.SkippedTroops}, rosters skipped={report.SkippedRosters}."
            );

            RefreshUiAfterImport();

            // Auto-open appropriate page after confirmation/import.
            LaunchEditorAfterImport(target);
        }

        /// <summary>
        /// Fires refresh events after a successful import.
        /// </summary>
        private static void RefreshUiAfterImport()
        {
            EventManager.Fire(UIEvent.Tree);
            EventManager.Fire(UIEvent.Library);
            EventManager.Fire(UIEvent.Page);
        }

        private static string BuildTroopTitle(WCharacter t)
        {
            if (t == null)
                return string.Empty;

            var tier = t.Tier;
            return tier > 0 ? $"{t.Name} (T{tier})" : t.Name;
        }

        /// <summary>
        /// Re-opens the editor on the imported selection (faction and optionally character).
        /// </summary>
        private static void LaunchEditorAfterImport(
            IBaseFaction faction,
            WCharacter character = null
        )
        {
            try
            {
                faction ??= Player.Clan;

                var args = EditorLaunchArgs.ForMode(EditorState.Instance.Mode);

                // Use reflection to avoid depending on writable properties / overloads.
                Reflection.SetPropertyValue(args, "Faction", faction);

                if (character != null)
                    Reflection.SetPropertyValue(args, "Character", character);

                EditorLauncher.Launch(args);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.LaunchEditorAfterImport failed.");
            }
        }
    }
}
