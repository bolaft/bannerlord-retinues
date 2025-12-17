using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Helpers;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers
{
    public class CharacterController : BaseController
    {
        /// <summary>
        /// Change the name of the selected character.
        /// </summary>
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

        /// <summary>
        /// Change the culture of the selected character.
        /// </summary>
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

        /// <summary>
        /// Toggle the gender of the selected character.
        /// </summary>
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

        /// <summary>
        /// Determines whether the current character can be removed.
        /// </summary>
        public static bool CanRemoveCharacter(out TextObject reason) =>
            Check(
                [
                    (
                        () => State.Character.IsHero == false,
                        L.T("character_cannot_remove_hero", "Heroes cannot be removed.")
                    ),
                    (
                        () => State.Character.UpgradeTargets.Count == 0,
                        L.T(
                            "character_cannot_remove_with_upgrades",
                            "Can't remove a unit that still has upgrades."
                        )
                    ),
                    (
                        () => !State.Character.IsRoot,
                        L.T("character_cannot_remove_root", "Root units cannot be removed.")
                    ),
                ],
                out reason
            );

        public static void RemoveCharacter()
        {
            if (!CanRemoveCharacter(out var reason))
            {
                Inquiries.Popup(L.T("cannot_remove_character_title", "Cannot Remove Unit"), reason);
                return;
            }

            Inquiries.Popup(
                title: L.T("inquiry_confirm_remove_character_title", "Delete Unit"),
                description: L.T(
                        "inquiry_confirm_remove_character_text",
                        "Are you sure you want to delete {UNIT_NAME}? This action cannot be undone."
                    )
                    .SetTextVariable("UNIT_NAME", State.Character.Name.ToString()),
                onConfirm: () =>
                {
                    var character = State.Character;

                    // 1) Select another character first.
                    State.Character = State.Faction.Troops.FirstOrDefault(c => c != character);

                    // 2) Remove from faction.
                    character.Remove();

                    // 3) Notify the UI.
                    EventManager.Fire(UIEvent.Tree, EventScope.Global);
                }
            );
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

            var races = GetValidRacesForCurrentSelection();
            if (races.Count == 0)
                return;

            var names = GetRaceNames();

            var elements = new List<InquiryElement>(races.Count);
            foreach (var race in races)
            {
                string title = null;

                if (names != null && race >= 0 && race < names.Length)
                    title = FormatRaceName(names[race]);

                title ??= $"Race {race}";

                elements.Add(new InquiryElement(race, title, null));
            }

            Inquiries.SelectPopup(
                title: L.T("change_species_title", "Change Species"),
                elements: elements,
                onSelect: element =>
                {
                    if (element?.Identifier is int race)
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
                    wc.Race = newRace;
                    return true;
                })
            )
                return;

            EventManager.Fire(UIEvent.Culture, EventScope.Local);
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

            // 1) Must be a valid race id (FaceGen knows it).
            try
            {
                if (FaceGen.GetBaseMonsterFromRace(wc.Race) == null)
                    return false;
            }
            catch
            {
                return false;
            }

            // 2) Culture must actually have a roster that exposes this race (for current gender).
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

            if (IsValidSpeciesCombo(wc))
                return true;

            snap.Restore(wc);

            Inquiries.Popup(
                L.T("no_valid_model_title_species", "No Valid Model"),
                L.T(
                    "no_valid_model_body_species",
                    "This combination of gender, culture and species does not have a valid model.\n\nThe previous appearance has been restored."
                )
            );

            return false;
        }
    }
}
