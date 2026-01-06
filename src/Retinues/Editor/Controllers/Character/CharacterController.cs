using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.Services;
using Retinues.Framework.Model.Exports;
using Retinues.Modules;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
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

        private static void RenameImpl(WCharacter c)
        {
            if (c == null)
                return;

            static void Apply(ICharacter target, string newName)
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
                    _ => WCulture.All != null && WCulture.All.Any(),
                    L.T("no_cultures_text", "No cultures are loaded in the current game.")
                )
                .DefaultTooltip(L.T("change_culture_title", "Change Culture"))
                .ExecuteWith(SelectCultureImpl);

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

        private static bool ApplyCulture(ICharacter character, WCulture newCulture)
        {
            if (character == null)
                return false;

            if (newCulture == character.Culture)
                return false;

            if (
                !TryApplyAppearanceChange(() =>
                {
                    character.Culture = newCulture;

                    if (character is WCharacter wc)
                        wc.ApplyCultureBodyProperties();

                    return true;
                })
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
                    applies: _ => HasAlternateSpecies() && State.Character?.Editable is WCharacter,
                    test: c => c?.Culture != null,
                    reason: L.T("gender_no_culture", "No culture is selected.")
                )
                .AddCondition(
                    applies: _ => HasAlternateSpecies() && State.Character?.Editable is WCharacter,
                    test: c =>
                    {
                        if (c == null)
                            return true;

                        var targetFemale = !c.IsFemale;
                        return FindTemplate(c.Culture, targetFemale, c.Race) != null;
                    },
                    reason: L.T(
                        "gender_no_template",
                        "This culture has no valid body template for that gender/species."
                    )
                )
                .AddCondition(
                    applies: _ => HasAlternateSpecies() && State.Character?.Editable is WCharacter,
                    test: c =>
                    {
                        if (c == null)
                            return true;

                        var targetFemale = !c.IsFemale;
                        return IsRenderable(c.Culture, targetFemale, c.Race);
                    },
                    reason: L.T(
                        "gender_not_renderable",
                        "That gender/species combination cannot be rendered."
                    )
                )
                .DefaultTooltip(L.T("gender_toggle_hint", "Change Gender"))
                .ExecuteWith(c => ToggleGenderImpl((c ?? State.Character)?.Editable))
                .Fire(UIEvent.Gender);

        private static void ToggleGenderImpl(ICharacter character)
        {
            if (character == null)
                return;

            if (
                !TryApplyAppearanceChange(() =>
                {
                    character.IsFemale = !character.IsFemale;

                    if (character is WCharacter wc)
                        wc.ApplyCultureBodyProperties();

                    return true;
                })
            )
                return;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Race                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static string[] _raceNamesCache;

        static bool HasAlternateSpecies() => FaceGen.GetRaceCount() > 1;

        static string[] GetRaceNames() => _raceNamesCache ??= FaceGen.GetRaceNames() ?? [];

        static string FormatRaceName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Replace('_', ' ').Trim();

            if (raw.Length == 1)
                return raw.ToUpperInvariant();

            return char.ToUpperInvariant(raw[0]) + raw.Substring(1);
        }

        public static bool CanChangeRace =>
            HasAlternateSpecies() && State.Character?.Editable is WCharacter;

        public static string GetRaceText()
        {
            if (State.Character?.Editable is not WCharacter wc)
                return null;

            var names = GetRaceNames();
            int r = wc.Race;

            if (names != null && r >= 0 && r < names.Length)
                return FormatRaceName(names[r]) ?? $"Race {r}";

            return $"Race {r}";
        }

        public static EditorAction<WCharacter> SelectRace { get; } =
            Action<WCharacter>("SelectRace")
                .AddCondition(
                    _ => CanChangeRace,
                    L.T("race_cannot_change", "Species cannot be changed for this unit.")
                )
                .DefaultTooltip(L.T("change_species_title", "Change Species"))
                .ExecuteWith(SelectRaceImpl);

        private static void SelectRaceImpl(WCharacter wc)
        {
            if (!CanChangeRace)
                return;

            if (wc == null)
                return;

            int raceCount;
            try
            {
                raceCount = FaceGen.GetRaceCount();
            }
            catch
            {
                return;
            }

            if (raceCount <= 0)
                return;

            var names = GetRaceNames();

            var valid = new HashSet<int>(GetValidRacesFor(wc.Culture, wc.IsFemale));

            bool HasTemplateForRace(int race) =>
                wc.Culture != null
                && wc.Culture.Troops.Any(t =>
                    t != null && t.IsFemale == wc.IsFemale && t.Race == race
                );

            bool IsRaceModelValid(int race)
            {
                try
                {
                    return FaceGen.GetBaseMonsterFromRace(race) != null;
                }
                catch
                {
                    return false;
                }
            }

            bool IsRaceCompatible(int race) => valid.Count == 0 || valid.Contains(race);

            string GetRaceTitle(int race)
            {
                string title = null;

                if (names != null && race >= 0 && race < names.Length)
                    title = FormatRaceName(names[race]);

                return title ?? $"Race {race}";
            }

            var elements = new List<InquiryElement>(raceCount);

            for (int race = 0; race < raceCount; race++)
            {
                var enabled = Check(
                    [
                        (
                            () => IsRaceModelValid(race),
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
                            () => HasTemplateForRace(race),
                            L.T(
                                "race_incompatible_culture_gender",
                                "This species is not compatible with the current culture/gender."
                            )
                        ),
                        (
                            () => IsRenderable(wc.Culture, wc.IsFemale, race),
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

        private static bool ApplyRace(WCharacter wc, int newRace)
        {
            if (!CanChangeRace)
                return false;

            if (wc == null)
                return false;

            if (newRace == wc.Race)
                return false;

            if (!TryApplyAppearanceChange(() => wc.ApplyCultureBodyPropertiesForRace(newRace)))
                return false;

            return true;
        }

        static List<int> GetValidRacesFor(WCulture culture, bool isFemale)
        {
            if (culture == null)
                return [];

            var set = new HashSet<int>();

            foreach (var troop in culture.Troops)
            {
                if (troop == null)
                    continue;

                if (troop.IsFemale != isFemale)
                    continue;

                set.Add(troop.Race);
            }

            if (set.Count == 0)
            {
                foreach (var troop in culture.Troops)
                {
                    if (troop == null)
                        continue;

                    set.Add(troop.Race);
                }
            }

            var list = new List<int>(set);
            list.Sort();
            return list;
        }

        static readonly Dictionary<string, bool> _renderableCache = new();

        static string RenderKey(WCulture culture, bool isFemale, int race)
        {
            var c = culture?.StringId ?? "null";
            return $"{c}|{(isFemale ? "F" : "M")}|{race}";
        }

        static WCharacter FindTemplate(WCulture culture, bool isFemale, int race)
        {
            if (culture == null)
                return null;

            var root = culture.RootBasic ?? culture.RootElite;
            if (root != null && root.IsFemale == isFemale && root.Race == race)
                return root;

            var villager = isFemale ? culture.VillageWoman : culture.Villager;
            if (villager != null && villager.IsFemale == isFemale && villager.Race == race)
                return villager;

            foreach (var troop in culture.Troops)
            {
                if (troop == null)
                    continue;

                if (troop.IsFemale == isFemale && troop.Race == race)
                    return troop;
            }

            return null;
        }

        static bool IsRenderable(WCulture culture, bool isFemale, int race)
        {
            var key = RenderKey(culture, isFemale, race);
            if (_renderableCache.TryGetValue(key, out var cached))
                return cached;

            var template = FindTemplate(culture, isFemale, race);
            if (template?.Base == null)
            {
                _renderableCache[key] = false;
                return false;
            }

            try
            {
                var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
                vm.FillFrom(template.Base, seed: 0);

                _renderableCache[key] = true;
                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                _renderableCache[key] = false;
                return false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Appearance Guard                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        readonly struct AppearanceSnapshot(WCharacter wc)
        {
            public readonly WCulture Culture = wc?.Culture;
            public readonly bool IsFemale = wc?.IsFemale ?? false;
            public readonly int Race = wc?.Race ?? 0;
            public readonly string BodyEnvelope = wc?.SerializeBodyEnvelope();

            public void Restore(WCharacter wc)
            {
                if (wc == null)
                    return;

                try
                {
                    if (Culture != null)
                        wc.Culture = Culture;

                    wc.IsFemale = IsFemale;
                    wc.Race = Race;

                    if (!string.IsNullOrEmpty(BodyEnvelope))
                        wc.ApplySerializedBodyEnvelope(BodyEnvelope);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }
        }

        static bool IsValidSpeciesCombo(WCharacter wc)
        {
            if (wc == null)
                return true;

            if (!HasAlternateSpecies())
                return true;

            try
            {
                if (FaceGen.GetBaseMonsterFromRace(wc.Race) == null)
                    return false;
            }
            catch
            {
                return false;
            }

            var valid = GetValidRacesFor(wc.Culture, wc.IsFemale);
            if (valid.Count == 0)
                return true;

            return valid.Contains(wc.Race);
        }

        static bool TryApplyAppearanceChange(Func<bool> applyChange)
        {
            if (applyChange == null)
                return false;

            if (!HasAlternateSpecies())
                return applyChange();

            if (State.Character?.Editable is not WCharacter wc)
                return applyChange();

            var snap = new AppearanceSnapshot(wc);

            bool changed;
            try
            {
                changed = applyChange();
            }
            catch (Exception e)
            {
                Log.Exception(e);
                snap.Restore(wc);
                return false;
            }

            if (!changed)
                return false;

            if (!IsRenderable(wc.Culture, wc.IsFemale, wc.Race))
            {
                snap.Restore(wc);

                Inquiries.Popup(
                    L.T("no_valid_model_title_species", "No Valid Model"),
                    L.T(
                        "no_valid_model_body_species",
                        "This combination of gender, culture and species cannot be rendered.\n\nThe previous appearance has been restored."
                    )
                );

                return false;
            }

            if (IsValidSpeciesCombo(wc))
                return true;

            snap.Restore(wc);

            Inquiries.Popup(
                L.T("no_valid_model_title_species", "No Valid Model"),
                L.T(
                    "no_valid_model_body_species",
                    "This combination of gender, culture and species cannot be rendered.\n\nThe previous appearance has been restored."
                )
            );

            return false;
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
