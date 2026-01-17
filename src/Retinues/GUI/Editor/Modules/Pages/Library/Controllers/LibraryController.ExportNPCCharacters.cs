using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Retinues.Domain.Characters.Services.Exports.NpcCharacters;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Services.Exports.NpcCharacters;
using Retinues.Framework.Model.Exports;
using Retinues.Framework.Modules.Mods;
using Retinues.GUI.Editor.Services.Library.NPCCharacters;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.Controllers.Library
{
    public partial class LibraryController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Convert to NPCCharacters               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Confirms before writing a standalone module.
        /// </summary>
        private static void ExecuteExportNpcCharactersWithConfirm(MLibrary.Item item)
        {
            if (item == null)
                return;

            if (
                !ModEnvironment.TryGetGameModulesDirectory(out var modulesDir)
                || string.IsNullOrWhiteSpace(modulesDir)
            )
            {
                Inquiries.Popup(
                    title: L.T("library_export_npc_failed_title", "Export Failed"),
                    description: L.T(
                        "library_export_npc_failed_no_modules_dir",
                        "Could not locate the game's Modules folder."
                    )
                );
                return;
            }

            if (
                !LibraryExportPayloadReader.TryExtractModelCharacterPayloads(item, out var payloads)
                || payloads.Count == 0
            )
            {
                Inquiries.Popup(
                    title: L.T("library_export_npc_failed_title", "Export Failed"),
                    description: L.T(
                        "library_export_npc_failed_no_payload",
                        "Nothing could be exported from this file."
                    )
                );
                return;
            }

            var moduleId = BuildNpcModuleId(item);
            var moduleRoot = Path.Combine(modulesDir, moduleId);

            var fallbackRoot = FileSystem.GetPathInRetinuesDocuments("GeneratedMods", moduleId);
            var overwriting = Directory.Exists(moduleRoot);

            var shown = payloads
                .Select(p => p.ModelStringId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(5)
                .ToList();

            var suffix =
                payloads.Count > shown.Count
                    ? $" (+{payloads.Count - shown.Count} more)"
                    : string.Empty;

            var list =
                shown.Count > 0
                    ? "\n\nCharacters:\n"
                        + string.Join("\n", shown.Select(id => "- " + id))
                        + suffix
                    : string.Empty;

            var overwriteLine = overwriting
                ? "\n\nWarning: An existing module with this id already exists and will be overwritten."
                : string.Empty;

            var desc = L.T(
                    "library_export_npc_confirm_desc",
                    "This will generate a standalone module.\n\n- Module id: {ID}\n- Target folder: {PATH}\n- Characters: {COUNT}{LIST}\n\nIf the game install folder is protected, the module will be written here instead:\n{FALLBACK}{OVERWRITE}\n\nContinue?"
                )
                .SetTextVariable("ID", moduleId)
                .SetTextVariable("PATH", moduleRoot)
                .SetTextVariable("COUNT", payloads.Count)
                .SetTextVariable("LIST", list)
                .SetTextVariable("FALLBACK", fallbackRoot)
                .SetTextVariable("OVERWRITE", overwriteLine);

            Inquiries.Popup(
                title: L.T("library_export_npc_confirm_title", "Convert to Standalone Mod"),
                description: desc,
                onConfirm: () => ApplyExportNpcCharacters(item)
            );
        }

        /// <summary>
        /// Writes a standalone NPCCharacters module from the selected export.
        /// </summary>
        private static void ApplyExportNpcCharacters(MLibrary.Item item)
        {
            try
            {
                if (item == null)
                    return;

                if (
                    !ModEnvironment.TryGetGameModulesDirectory(out var modulesDir)
                    || string.IsNullOrWhiteSpace(modulesDir)
                )
                {
                    Inquiries.Popup(
                        title: L.T("library_export_npc_failed_title", "Export Failed"),
                        description: L.T(
                            "library_export_npc_failed_no_modules_dir",
                            "Could not locate the game's Modules folder."
                        )
                    );
                    return;
                }

                if (
                    !LibraryExportPayloadReader.TryExtractModelCharacterPayloads(
                        item,
                        out var payloads
                    )
                    || payloads.Count == 0
                )
                {
                    Inquiries.Popup(
                        title: L.T("library_export_npc_failed_title", "Export Failed"),
                        description: L.T(
                            "library_export_npc_failed_no_payload",
                            "Nothing could be exported from this file."
                        )
                    );
                    return;
                }

                var npcStrings = new List<string>();
                var npcIds = new List<string>();
                var missingVanillaBases = new List<string>();

                foreach (var p in payloads)
                {
                    using var lease = CharacterStubLeaser.LeaseFromPayload(
                        p.Payload,
                        p.ModelStringId,
                        out var missingVanillaBaseId
                    );

                    if (lease == null || lease.Character == null)
                        continue;

                    if (!string.IsNullOrWhiteSpace(missingVanillaBaseId))
                        missingVanillaBases.Add(missingVanillaBaseId);

                    if (
                        !TryBuildNpcCharacterXml(
                            lease.Character,
                            p.ModelStringId,
                            out var npcXml,
                            out var npcId
                        )
                    )
                        continue;

                    npcStrings.Add(npcXml);
                    npcIds.Add(npcId);
                }

                if (npcStrings.Count == 0)
                {
                    Inquiries.Popup(
                        title: L.T("library_export_npc_failed_title", "Export Failed"),
                        description: L.T(
                            "library_export_npc_failed_no_output",
                            "No characters could be generated from this export."
                        )
                    );
                    return;
                }

                var moduleId = BuildNpcModuleId(item);

                var project = NpcCharactersStandaloneModBuilder.BuildNpcCharactersModProject(
                    moduleId,
                    npcStrings,
                    npcIds
                );

                var writer = new ModWriter();

                // First try to write to the game's Modules folder.
                var moduleRoot = Path.Combine(modulesDir, moduleId);
                var result = writer.WriteToDirectory(moduleRoot, project, overwrite: true);

                if (!result.Success && result.Exception is UnauthorizedAccessException)
                {
                    // Program Files (x86) is commonly protected.
                    var fallbackRoot = FileSystem.GetPathInRetinuesDocuments(
                        "GeneratedMods",
                        moduleId
                    );
                    var fallbackResult = writer.WriteToDirectory(
                        fallbackRoot,
                        project,
                        overwrite: true
                    );

                    if (!fallbackResult.Success)
                    {
                        Inquiries.Popup(
                            title: L.T("library_export_npc_failed_title", "Export Failed"),
                            description: L.T(
                                "library_export_npc_failed_exception",
                                "The file could not be exported."
                            )
                        );
                        return;
                    }

                    ShowNpcExportDonePopup(
                        titleKey: "library_export_npc_done_title",
                        titleFallback: "Export Complete",
                        descKey: "library_export_npc_done_desc_fallback",
                        descFallback: "The game install folder is protected, so the module was written here instead:\n{PATH}\n\nCopy this folder into your Bannerlord Modules directory, then restart the game.",
                        path: fallbackRoot,
                        missingVanillaBases: missingVanillaBases
                    );

                    return;
                }

                if (!result.Success)
                {
                    Inquiries.Popup(
                        title: L.T("library_export_npc_failed_title", "Export Failed"),
                        description: L.T(
                            "library_export_npc_failed_exception",
                            "The file could not be exported."
                        )
                    );
                    return;
                }

                ShowNpcExportDonePopup(
                    titleKey: "library_export_npc_done_title",
                    titleFallback: "Export Complete",
                    descKey: "library_export_npc_done_desc_mod",
                    descFallback: "Standalone module written:\n{PATH}\n\nRestart the game to load it.",
                    path: moduleRoot,
                    missingVanillaBases: missingVanillaBases
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "LibraryController.ApplyExportNpcCharacters failed.");
                Inquiries.Popup(
                    title: L.T("library_export_npc_failed_title", "Export Failed"),
                    description: L.T(
                        "library_export_npc_failed_exception",
                        "The file could not be exported."
                    )
                );
            }
        }

        /// <summary>
        /// Builds NPC XML from a character export payload.
        /// </summary>
        private static bool TryBuildNpcCharacterXml(
            WCharacter character,
            string overrideId,
            out string npcXml,
            out string npcId
        )
        {
            npcXml = null;
            npcId = null;

            if (character == null)
                return false;

            npcId = !string.IsNullOrWhiteSpace(overrideId) ? overrideId : character.StringId;
            if (string.IsNullOrWhiteSpace(npcId))
                return false;

            npcXml = NpcCharacterXmlExporter.ExportAsNpc(character, npcId);
            return !string.IsNullOrWhiteSpace(npcXml);
        }

        /// <summary>
        /// Builds a module id for NPC export.
        /// </summary>
        private static string BuildNpcModuleId(MLibrary.Item item)
        {
            // Unique-enough id based on export's source id.
            var raw = item?.SourceId;

            if (string.IsNullOrWhiteSpace(raw))
                raw = item?.DisplayName;

            if (string.IsNullOrWhiteSpace(raw))
                raw = item?.SourceId;

            if (string.IsNullOrWhiteSpace(raw))
                raw = "Export";

            var suffix = ModEnvironment.SanitizeModuleId(raw, fallback: "Export");
            return NpcExportModulePrefix + suffix;
        }

        private static void ShowNpcExportDonePopup(
            string titleKey,
            string titleFallback,
            string descKey,
            string descFallback,
            string path,
            List<string> missingVanillaBases
        )
        {
            var warn =
                missingVanillaBases != null && missingVanillaBases.Count > 0
                    ? "\n\nWarnings:\nSome vanilla baselines could not be resolved (delta exports may be incomplete):\n"
                        + string.Join("\n", missingVanillaBases.Distinct())
                    : string.Empty;

            var baseDesc = L.T(descKey, descFallback).SetTextVariable("PATH", path).ToString();

            Inquiries.Popup(
                title: L.T(titleKey, titleFallback),
                description: new TextObject(baseDesc + warn)
            );
        }
    }
}
