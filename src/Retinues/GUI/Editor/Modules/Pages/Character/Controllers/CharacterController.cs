using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Retinues.Compatibility;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Exports;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Modules.Pages.Library.Services;
using Retinues.GUI.Editor.Shared.Controllers;
using Retinues.GUI.Editor.Shared.Services.Appearance;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.Modules.Pages.Character.Controllers
{
    /// <summary>
    /// Controller for character editing actions and related UI flows.
    /// </summary>
    public class CharacterController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Export                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Exports the provided character to an XML document and initiates the save flow.
        /// </summary>
        public static ControllerAction<WCharacter> ExportCharacter { get; } =
            Action<WCharacter>("ExportCharacter")
                .AddCondition(
                    c => !c.IsHero,
                    L.T("export_character_no_heroes", "Hero data is tied to the save state.")
                )
                .DefaultTooltip(
                    L.T(
                        "button_export_character_tooltip",
                        "Save this character and add it to the library."
                    )
                )
                .ExecuteWith(ExportCharacterImpl);

        /// <summary>
        /// Builds the export document for the character and writes it to disk after user confirmation.
        /// </summary>
        private static void ExportCharacterImpl(WCharacter c)
        {
            if (c == null)
                return;

            if (!CharacterExporter.TryBuildExport(c, out var doc, out var err))
            {
                Inquiries.Popup(
                    title: L.T("export_failed_title", "Export Failed"),
                    description: L.T("export_failed_generic", "Could not export: {ERROR}.")
                        .SetTextVariable("ERROR", err ?? L.S("unknown_error", "Unknown error."))
                );

                return;
            }

            var defaultName = c.Name;
            if (string.IsNullOrWhiteSpace(defaultName))
                defaultName = c.StringId;

            Inquiries.TextInputPopup(
                title: L.T("export_name_title", "Export Name"),
                description: L.T("export_name_desc", "Choose a file name for this export:"),
                defaultInput: defaultName,
                onConfirm: input =>
                {
                    input = (input ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        Inquiries.Popup(
                            L.T("invalid_name_title", "Invalid Name"),
                            L.T("invalid_name_body", "The name cannot be empty.")
                        );
                        return;
                    }

                    if (
                        !TryBuildExportPath(
                            input,
                            out var path,
                            out var safeFileName,
                            out var error
                        )
                    )
                    {
                        Inquiries.Popup(
                            L.T("invalid_name_title", "Invalid Name"),
                            error ?? L.T("invalid_name_body", "The name cannot be empty.")
                        );
                        return;
                    }

                    void Write()
                    {
                        try
                        {
                            var dir = Path.GetDirectoryName(path);
                            if (!string.IsNullOrWhiteSpace(dir))
                                Directory.CreateDirectory(dir);

                            File.WriteAllText(path, doc.ToString(), new UTF8Encoding(false));

                            Inquiries.Popup(
                                title: L.T("export_done_title", "Export Complete"),
                                description: L.T("export_done_desc", "Export written to:\n{PATH}")
                                    .SetTextVariable("PATH", path)
                            );

                            EventManager.Fire(UIEvent.Library);
                        }
                        catch (Exception ex)
                        {
                            Log.Exception(ex);

                            Inquiries.Popup(
                                title: L.T("export_failed_title", "Export Failed"),
                                description: L.T(
                                    "export_failed_exception",
                                    "The file could not be written."
                                )
                            );
                        }
                    }

                    if (File.Exists(path))
                    {
                        Inquiries.Popup(
                            title: L.T("export_overwrite_title", "File Already Exists"),
                            description: L.T(
                                    "export_overwrite_desc",
                                    "A file named '{NAME}' already exists.\n\nDo you want to overwrite it?"
                                )
                                .SetTextVariable("NAME", safeFileName),
                            confirmText: L.T("overwrite_confirm", "Overwrite"),
                            cancelText: GameTexts.FindText("str_cancel"),
                            onConfirm: Write
                        );

                        return;
                    }

                    Write();
                }
            );
        }

        /// <summary>
        /// Builds a safe file path for the export name and returns validation errors if any.
        /// </summary>
        private static bool TryBuildExportPath(
            string inputFileName,
            out string path,
            out string safeFileName,
            out TextObject error
        )
        {
            path = null;
            safeFileName = null;
            error = null;

            var raw = (inputFileName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = L.T("invalid_name_body", "The name cannot be empty.");
                return false;
            }

            var safe = FileSystem.SanitizeFileName(raw, string.Empty);
            safe = (safe ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(safe))
            {
                error = L.T(
                    "invalid_name_sanitize",
                    "This name contains no valid file characters."
                );
                return false;
            }

            if (!safe.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                safe += ".xml";

            safeFileName = safe;

            var dir = ExportLibrary.ExportDirectory;
            path = Path.Combine(dir, safeFileName);

            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           Name                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Prompt for a new name and rename the selected character.
        /// </summary>
        public static ControllerAction<WCharacter> Rename { get; } =
            Action<WCharacter>("Rename")
                .DefaultTooltip(L.T("rename_tooltip", "Rename"))
                .ExecuteWith(RenameImpl);

        /// <summary>
        /// Rename the given character.
        /// </summary>
        private static void RenameImpl(WCharacter c)
        {
            if (c == null)
                return;

            static void Apply(Domain.Characters.ICharacterData target, string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    Inquiries.Popup(
                        L.T("invalid_name_title", "Invalid Name"),
                        L.T("invalid_name_body", "The name cannot be empty.")
                    );
                    return;
                }

                newName = newName.Trim();
                if (newName == target.Name)
                    return;

                target.Name = newName;

                EventManager.Fire(UIEvent.Name);
            }

            Inquiries.TextInputPopup(
                title: L.T("rename_unit", "New Name"),
                defaultInput: c.Editable.Name,
                onConfirm: input => Apply(c.Editable, input),
                description: L.T("enter_new_name", "Enter a new name:")
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Opens the culture picker for the selected character.
        /// </summary>
        public static ControllerAction<WCharacter> SelectCulture { get; } =
            Action<WCharacter>("SelectCulture")
                .AddCondition(
                    _ => !State.Character.IsRetinue,
                    L.T("cant_change_retinue_culture", "Retinues cannot change culture.")
                )
                .AddCondition(
                    _ => WCulture.All != null && WCulture.All.Any(),
                    L.T("no_cultures_text", "No cultures are loaded in the current game.")
                )
                .DefaultTooltip(L.T("change_culture_title", "Change Culture"))
                .ExecuteWith(SelectCultureImpl);

        /// <summary>
        /// Show the culture picker for the given character.
        /// </summary>
        private static void SelectCultureImpl(WCharacter c)
        {
            if (c == null)
                return;

            var elements = new List<InquiryElement>();

            foreach (var culture in WCulture.All)
            {
                var imageIdentifier = culture?.ImageIdentifier;
                var name = culture?.Name;

                if (imageIdentifier == null || name == null)
                    continue;

                elements.Add(
                    new InquiryElement(
                        identifier: culture,
                        title: name,
                        imageIdentifier: imageIdentifier
                    )
                );
            }

            if (elements.Count == 0)
            {
                Inquiries.Popup(
                    L.T("no_cultures_title", "No Cultures Found"),
                    L.T("no_cultures_text", "No cultures are loaded in the current game.")
                );
                return;
            }

            Inquiries.SelectPopup(
                title: L.T("change_culture_title", "Change Culture"),
                elements: elements,
                onSelect: element =>
                {
                    if (element?.Identifier is not WCulture newCulture)
                        return;

                    if (ApplyCulture(c.Editable, newCulture))
                        EventManager.Fire(UIEvent.Culture);
                }
            );
        }

        /// <summary>
        /// Apply the selected culture to the character.
        /// </summary>
        private static bool ApplyCulture(
            Domain.Characters.ICharacterData character,
            WCulture newCulture
        )
        {
            if (character == null)
                return false;

            if (newCulture == character.Culture)
                return false;

            if (
                !AppearanceGuard.TryApply(
                    () =>
                    {
                        character.Culture = newCulture;

                        if (character is WCharacter wc)
                            wc.ApplyCultureBodyProperties();

                        return true;
                    },
                    character as WCharacter
                )
            )
                return false;

            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Race                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Opens the species/race picker for the selected character.
        /// </summary>
        public static ControllerAction<WCharacter> SelectRace { get; } =
            Action<WCharacter>("SelectRace")
                .AddCondition(
                    _ => CanChangeRace,
                    L.T("race_cannot_change", "Species cannot be changed for this unit.")
                )
                .DefaultTooltip(L.T("change_species_title", "Change Species"))
                .ExecuteWith(SelectRaceImpl);

        /// <summary>
        /// Show the race picker for the given character.
        /// </summary>
        private static void SelectRaceImpl(WCharacter wc)
        {
            if (!CanChangeRace)
                return;

            if (wc == null)
                return;

            var raceCount = RaceHelper.GetRaceCount();
            if (raceCount <= 0)
                return;

            var names = RaceHelper.GetRaceNames();
            var valid = new HashSet<int>(RaceHelper.GetValidRacesFor(wc.Culture, wc.IsFemale));

            string GetRaceTitle(int race)
            {
                string title = null;

                if (names != null && race >= 0 && race < names.Length)
                    title = RaceHelper.FormatRaceName(names[race]);

                return title ?? $"Race {race}";
            }

            bool IsRaceCompatible(int race) => valid.Count == 0 || valid.Contains(race);

            var elements = new List<InquiryElement>(raceCount);

            for (int race = 0; race < raceCount; race++)
            {
                var enabled = Check(
                    [
                        (
                            () => RaceHelper.IsRaceModelValid(race),
                            L.T("race_invalid_model", "No valid model exists for this species.")
                        ),
                        (
                            () => IsRaceCompatible(race),
                            L.T(
                                "race_incompatible_culture_gender",
                                "This species is not compatible with the current culture/gender."
                            )
                        ),
                        (
                            () => RaceHelper.HasTemplateForRace(wc.Culture, wc.IsFemale, race),
                            L.T(
                                "race_incompatible_culture_gender",
                                "This species is not compatible with the current culture/gender."
                            )
                        ),
                        (
                            () => AppearanceGuard.CanRender(wc.Culture, wc.IsFemale, race),
                            L.T(
                                "race_incompatible_culture_gender",
                                "This species is not compatible with the current culture/gender."
                            )
                        ),
                    ],
                    out TextObject reason
                );

                elements.Add(
                    new InquiryElement(
                        identifier: race,
                        title: GetRaceTitle(race),
                        imageIdentifier: null,
                        isEnabled: enabled,
                        hint: enabled ? null : reason?.ToString()
                    )
                );
            }

            Inquiries.SelectPopup(
                title: L.T("change_species_title", "Change Species"),
                elements: elements,
                onSelect: element =>
                {
                    if (element is not InquiryElement ie)
                        return;

                    if (!ie.IsEnabled)
                        return;

                    if (ie.Identifier is int race)
                    {
                        if (ApplyRace(wc, race))
                            EventManager.Fire(UIEvent.Culture);
                    }
                }
            );
        }

        /// <summary>
        /// Apply the selected race to the character.
        /// </summary>
        private static bool ApplyRace(WCharacter wc, int newRace)
        {
            if (!CanChangeRace)
                return false;

            if (wc == null)
                return false;

            if (newRace == wc.Race)
                return false;

            if (!AppearanceGuard.TryApply(() => wc.ApplyCultureBodyPropertiesForRace(newRace), wc))
                return false;

            return true;
        }

        /// <summary>
        /// Check if the race can be changed for the current character.
        /// </summary>
        public static bool CanChangeRace =>
            RaceHelper.HasAlternateSpecies() && State.Character?.Editable is WCharacter;

        /// <summary>
        /// Get the display text for the current character's race.
        /// </summary>
        public static string GetRaceText()
        {
            if (State.Character?.Editable is not WCharacter wc)
                return null;

            return RaceHelper.GetRaceName(wc);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Mixed Gender                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Toggles mixed-gender spawn allowance for the current character.
        /// </summary>
        public static ControllerAction<bool> SetMixedGender { get; } =
            Action<bool>("SetMixedGender")
                .RequireValidEditingContext()
                .AddCondition(
                    _ => State.Character.IsHero == false,
                    L.T("mixed_gender_hero_reason", "Not available to heroes.")
                )
                .DefaultTooltip(_ =>
                    State.Character?.IsMixedGender == true
                        ? L.T(
                            "mixed_gender_disable_tooltip",
                            "Disallow this unit from spawning as either male or female."
                        )
                        : L.T(
                            "mixed_gender_enable_tooltip",
                            "Allow this unit to spawn as either male or female."
                        )
                )
                .ExecuteWith(SetMixedGenderImpl)
                .Fire(UIEvent.Character);

        /// <summary>
        /// Set the mixed gender flag for the current character.
        /// </summary>
        private static void SetMixedGenderImpl(bool isMixedGender)
        {
            if (State.Character == null)
                return;

            if (State.Character.IsHero)
                return;

            State.Character.IsMixedGender = isMixedGender;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Mariner                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Toggles the mariner ability flag for the current character.
        /// </summary>
        public static ControllerAction<bool> SetMariner { get; } =
            Action<bool>("SetMariner")
                .RequireValidEditingContext()
                .AddCondition(
                    _ => Mods.NavalDLC.IsLoaded,
                    L.T("naval_dlc_not_loaded", "War Sails is not installed.")
                )
                .AddCondition(
                    _ => State.Character.IsHero == false,
                    L.T("mariner_hero_reason", "Heroes cannot be mariners.")
                )
                .DefaultTooltip(_ =>
                    State.Character.IsMariner
                        ? L.T(
                            "mariner_disable_tooltip",
                            "Disable the mariner ability for this unit."
                        )
                        : L.T("mariner_enable_tooltip", "Enable the mariner ability for this unit.")
                )
                .ExecuteWith(SetMarinerImpl)
                .Fire(UIEvent.Formation);

        /// <summary>
        /// Set the mariner flag for the current character.
        /// </summary>
        private static void SetMarinerImpl(bool isMariner)
        {
            if (!Mods.NavalDLC.IsLoaded)
                return;

            if (State.Character == null)
                return;

            if (State.Character.IsHero)
                return;

            State.Character.IsMariner = isMariner;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Rank Up                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ranks up the selected unit after confirming costs and requirements.
        /// </summary>
        public static ControllerAction<WCharacter> RankUp { get; } =
            Action<WCharacter>("RankUp")
                .RequireValidEditingContext()
                .AddCondition(
                    _ => State.Mode == EditorMode.Player,
                    L.T("rank_up_player_only", "Only available in player mode.")
                )
                .AddCondition(
                    c => c != null && c.IsRetinue,
                    L.T("rank_up_retinue_only", "Only available for retinues.")
                )
                .AddCondition(
                    c => c != null && !c.IsMaxTier,
                    L.T("rank_up_max_tier", "This unit is already at max tier.")
                )
                .AddCondition(
                    c => c != null && c.SkillTotalUsed >= c.SkillTotal,
                    L.T(
                        "rank_up_requires_maxed_skills",
                        "This unit must have maxed its total skills for its tier."
                    )
                )
                .AddCondition(
                    c => c != null && HasEnoughRankUpSkillPoints(c),
                    () =>
                        L.T("rank_up_not_enough_sp", "Not enough skill points (needs {POINTS}).")
                            .SetTextVariable("POINTS", GetRankUpSkillPointCost(State.Character))
                )
                .AddCondition(
                    c => c != null && HasEnoughGoldForRankUp(c),
                    () =>
                        L.T("rank_up_not_enough_money", "Not enough money (needs {COST} denars).")
                            .SetTextVariable("COST", GetRankUpGoldCost(State.Character))
                )
                .DefaultTooltip(L.T("rank_up_tooltip", "Rank up this unit."))
                .ExecuteWith(RankUpImpl)
                .Fire(UIEvent.Character);

        /// <summary>
        /// Returns the gold cost required to rank up the given character.
        /// </summary>
        private static int GetRankUpGoldCost(WCharacter c)
        {
            if (c == null)
                return 0;

            return (c.Tier + 1) * 1000;
        }

        /// <summary>
        /// Skill point cost: 5 per tier above 1 for the target tier.
        /// If we are tier N and we rank up to N+1, the cost is 5 * N.
        /// Examples:
        /// - tier 2 -> 3 costs 10
        /// - tier 3 -> 4 costs 15
        /// </summary>
        private static int GetRankUpSkillPointCost(WCharacter c)
        {
            if (c == null)
                return 0;

            // targetTier = c.Tier + 1 -> cost = 5 * (targetTier - 1) = 5 * c.Tier
            return 5 * c.Tier;
        }

        /// <summary>
        /// Returns true if the character has enough skill points to pay the rank-up cost.
        /// </summary>
        private static bool HasEnoughRankUpSkillPoints(WCharacter c)
        {
            if (c == null)
                return false;

            var cost = GetRankUpSkillPointCost(c);
            return c.SkillPoints >= cost;
        }

        /// <summary>
        /// Returns true if the main hero has enough gold to pay the rank-up cost for the character.
        /// </summary>
        private static bool HasEnoughGoldForRankUp(WCharacter c)
        {
            if (c == null)
                return false;

            var hero = Hero.MainHero;
            if (hero == null)
                return false;

            var cost = GetRankUpGoldCost(c);
            return hero.Gold >= cost;
        }

        /// <summary>
        /// Prompt for confirmation and perform the rank-up: deduct costs, increase level, and fire events.
        /// </summary>
        private static void RankUpImpl(WCharacter c)
        {
            if (c == null)
                return;

            var hero = Hero.MainHero;
            if (hero == null)
                return;

            var goldCost = GetRankUpGoldCost(c);
            var spCost = GetRankUpSkillPointCost(c);

            var body = L.T(
                    "rank_up_confirm_body",
                    "Rank up {NAME}?\n\nCosts:\n- {SP_COST} skill points\n- {GOLD_COST} gold\n\nEffect:\n- +5 levels (tier up)"
                )
                .SetTextVariable("NAME", c.Name)
                .SetTextVariable("SP_COST", spCost)
                .SetTextVariable("GOLD_COST", goldCost);

            Inquiries.Popup(
                title: L.T("rank_up_confirm_title", "Confirm Rank Up"),
                description: body,
                confirmText: L.T("rank_up_confirm_yes", "Rank Up"),
                cancelText: L.T("rank_up_confirm_no", "Cancel"),
                onConfirm: () =>
                {
                    if (!HasEnoughGoldForRankUp(c) || !HasEnoughRankUpSkillPoints(c))
                        return;

                    hero.ChangeHeroGold(-goldCost);
                    c.SkillPoints -= spCost;

                    c.Level += 5;

                    EventManager.Fire(UIEvent.Character);
                    EventManager.Fire(UIEvent.Skill);
                }
            );
        }
    }
}
