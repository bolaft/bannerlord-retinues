using System.Collections.Generic;
using System.Linq;
using Retinues.Compatibility;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Editor.MVC.Shared.Services.Appearance;
using Retinues.Interface.Services;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Editor.MVC.Pages.Character.Controllers
{
    /// <summary>
    /// Controller for character editing actions and related UI flows.
    /// </summary>
    public class CharacterController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           Name                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Prompt for a new name and rename the selected character.
        /// </summary>
        public static ControllerAction<WCharacter> Rename { get; } =
            Action<WCharacter>("Rename")
                .DefaultTooltip(L.T("rename_tooltip", "Rename."))
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
                .DefaultTooltip(L.T("change_culture_hint", "Change culture."))
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
                .DefaultTooltip(L.T("change_species_hint", "Change species."))
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
                    s => State.Character.IsCaptain != true,
                    L.T(
                        "mariner_captain_reason",
                        "Captains share the mariner ability of their base troops."
                    )
                )
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
    }
}
