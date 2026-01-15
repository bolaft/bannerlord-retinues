using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.Services.Appearance;
using Retinues.Editor.Services.Context;
using Retinues.Framework.Model.Exports;
using Retinues.Modules;
using Retinues.UI.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers.Character
{
    public class CharacterController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Export                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WCharacter> ExportCharacter { get; } =
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
        /// Export the given character.
        /// </summary>
        private static void ExportCharacterImpl(WCharacter c)
        {
            if (c == null)
                return;

            MImportExport.ExportCharacter(c.StringId);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           Name                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WCharacter> Rename { get; } =
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

        public static EditorAction<WCharacter> SelectCulture { get; } =
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
        //                         Gender                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WCharacter> ToggleGender { get; } =
            Action<WCharacter>("ToggleGender")
                .AddCondition(
                    applies: _ =>
                        RaceHelper.HasAlternateSpecies() && State.Character?.Editable is WCharacter,
                    test: c => c?.Culture != null,
                    reason: L.T("gender_no_culture", "No culture is selected.")
                )
                .AddCondition(
                    applies: _ =>
                        RaceHelper.HasAlternateSpecies() && State.Character?.Editable is WCharacter,
                    test: c =>
                    {
                        if (c == null)
                            return true;

                        var targetFemale = !c.IsFemale;
                        return RaceHelper.FindTemplate(c.Culture, targetFemale, c.Race) != null;
                    },
                    reason: L.T(
                        "gender_no_template",
                        "This culture has no valid body template for that gender/species."
                    )
                )
                .AddCondition(
                    applies: _ =>
                        RaceHelper.HasAlternateSpecies() && State.Character?.Editable is WCharacter,
                    test: c =>
                    {
                        if (c == null)
                            return true;

                        var targetFemale = !c.IsFemale;
                        return AppearanceGuard.CanRender(c.Culture, targetFemale, c.Race);
                    },
                    reason: L.T(
                        "gender_not_renderable",
                        "That gender/species combination cannot be rendered."
                    )
                )
                .DefaultTooltip(L.T("gender_toggle_hint", "Change Gender"))
                .ExecuteWith(c => ToggleGenderImpl((c ?? State.Character)?.Editable))
                .Fire(UIEvent.Gender);

        /// <summary>
        /// Toggle the gender of the given character.
        /// </summary>
        private static void ToggleGenderImpl(Domain.Characters.ICharacterData character)
        {
            if (character == null)
                return;

            AppearanceGuard.TryApply(
                () =>
                {
                    character.IsFemale = !character.IsFemale;

                    if (character is WCharacter wc)
                        wc.ApplyCultureBodyProperties();

                    return true;
                },
                character as WCharacter
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Race                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WCharacter> SelectRace { get; } =
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

        public static EditorAction<bool> SetMixedGender { get; } =
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

        public static EditorAction<bool> SetMariner { get; } =
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

        public static EditorAction<WCharacter> RankUp { get; } =
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
