using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Helpers;
using Retinues.Model;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Module;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers.Character
{
    public class CharacterController : EditorController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Export                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ExportSelectedCharacter()
        {
            var c = State.Character;
            if (c == null)
            {
                Notifications.Message("No character selected.");
                return;
            }

            MImportExport.ExportCharacter(c.StringId);

            EventManager.Fire(UIEvent.Library);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Name                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ChangeName()
        {
            static void Apply(string newName)
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    Inquiries.Popup(
                        L.T("invalid_name_title", "Invalid Name"),
                        L.T("invalid_name_body", "The name cannot be empty.")
                    );
                    return;
                }

                var character = State.Character.Editable;

                if (newName == character.Name)
                    return; // No change.

                character.Name = newName;
                EventManager.Fire(UIEvent.Name, EventScope.Local);
            }

            Inquiries.TextInputPopup(
                title: L.T("rename_unit", "New Name"),
                defaultInput: State.Character.Editable.Name,
                onConfirm: input => Apply(input.Trim()),
                description: L.T("enter_new_name", "Enter a new name:")
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Culture                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ChangeCulture(WCulture newCulture)
        {
            var character = State.Character.Editable;

            if (newCulture == character.Culture)
                return;

            if (
                !TryApplyAppearanceChange(() =>
                {
                    character.Culture = newCulture;

                    if (character is WCharacter wc)
                        wc.ApplyCultureBodyProperties();

                    return true;
                })
            )
                return;

            EventManager.Fire(UIEvent.Culture, EventScope.Local);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Gender                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<WCharacter> ToggleGender { get; } =
            Action<WCharacter>("ToggleGender")
                .AddCondition(
                    applies: _ => HasAlternateSpecies() && State.Character?.Editable is WCharacter,
                    test: _ => State.Character?.Editable?.Culture != null,
                    reason: L.T("gender_no_culture", "No culture is selected.")
                )
                .AddCondition(
                    applies: _ => HasAlternateSpecies() && State.Character?.Editable is WCharacter,
                    test: _ =>
                    {
                        var wc = State.Character.Editable as WCharacter;
                        if (wc == null)
                            return true;

                        var targetFemale = !wc.IsFemale;
                        return FindTemplate(wc.Culture, targetFemale, wc.Race) != null;
                    },
                    reason: L.T(
                        "gender_no_template",
                        "This culture has no valid body template for that gender/species."
                    )
                )
                .AddCondition(
                    applies: _ => HasAlternateSpecies() && State.Character?.Editable is WCharacter,
                    test: _ =>
                    {
                        var wc = State.Character.Editable as WCharacter;
                        if (wc == null)
                            return true;

                        var targetFemale = !wc.IsFemale;
                        return IsRenderable(wc.Culture, targetFemale, wc.Race);
                    },
                    reason: L.T(
                        "gender_not_renderable",
                        "That gender/species combination cannot be rendered."
                    )
                )
                .ExecuteWith(_ => ChangeGender());

        public static void ChangeGender()
        {
            var character = State.Character.Editable;

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

            EventManager.Fire(UIEvent.Gender, EventScope.Local);
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

        public static List<int> GetValidRacesForCurrentSelection()
        {
            if (State.Character?.Editable is not WCharacter wc)
                return [];

            return GetValidRacesFor(wc.Culture, wc.IsFemale);
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

            // Fallback if this culture has no troops for that gender.
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

        public static void OpenRaceSelector()
        {
            if (!CanChangeRace)
                return;

            if (State.Character?.Editable is not WCharacter wc)
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

            // Races that are "compatible" with the current selection.
            // If empty, we treat it as "no restriction".
            var valid = new HashSet<int>(GetValidRacesForCurrentSelection());

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

            bool IsRaceCompatible(int race)
            {
                // If we can't infer compatibility from rosters, don't block selection.
                return valid.Count == 0 || valid.Contains(race);
            }

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
                        ChangeRace(race);
                }
            );
        }

        public static void ChangeRace(int newRace)
        {
            if (!CanChangeRace)
                return;

            if (State.Character?.Editable is not WCharacter wc)
                return;

            if (newRace == wc.Race)
                return;

            if (
                !TryApplyAppearanceChange(() =>
                {
                    // Must update envelope/tags too, otherwise FaceGen can crash.
                    return wc.ApplyCultureBodyPropertiesForRace(newRace);
                })
            )
                return;

            EventManager.Fire(UIEvent.Culture, EventScope.Local);
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

        readonly struct AppearanceSnapshot
        {
            public readonly WCulture Culture;
            public readonly bool IsFemale;
            public readonly int Race;
            public readonly string BodyEnvelope;

            public AppearanceSnapshot(WCharacter wc)
            {
                Culture = wc?.Culture;
                IsFemale = wc?.IsFemale ?? false;
                Race = wc?.Race ?? 0;
                BodyEnvelope = wc?.SerializeBodyEnvelope();
            }

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
        //                         Mariner                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void ChangeMariner(bool isMariner)
        {
            if (!Mods.NavalDLC.IsLoaded)
                return; // Naval DLC not installed

            if (State.Character.IsHero)
                return; // Heroes cannot be mariners

            State.Character.IsMariner = isMariner;
            EventManager.Fire(UIEvent.Formation, EventScope.Local);
        }
    }
}
