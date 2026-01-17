using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;

namespace Retinues.GUI.Editor.Shared.Services.Appearance
{
    /// <summary>
    /// Service to guard against invalid appearance changes.
    /// </summary>
    public static class AppearanceGuard
    {
        /// <summary>
        /// Snapshot of a character's appearance.
        /// </summary>
        readonly struct AppearanceSnapshot(WCharacter wc)
        {
            public readonly WCulture Culture = wc?.Culture;
            public readonly bool IsFemale = wc?.IsFemale ?? false;
            public readonly int Race = wc?.Race ?? 0;
            public readonly string BodyEnvelope = wc?.SerializeBodyEnvelope();

            /// <summary>
            /// Restores the appearance to the snapshot state.
            /// </summary>
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

        static readonly Dictionary<string, bool> _renderableCache = [];

        /// <summary>
        /// Renders the cache key for the given appearance parameters.
        /// </summary>
        static string RenderKey(WCulture culture, bool isFemale, int race)
        {
            var c = culture?.StringId ?? "null";
            return $"{c}|{(isFemale ? "F" : "M")}|{race}";
        }

        /// <summary>
        /// Determines if the given appearance parameters can be rendered.
        /// </summary>
        public static bool CanRender(WCulture culture, bool isFemale, int race)
        {
            var key = RenderKey(culture, isFemale, race);
            if (_renderableCache.TryGetValue(key, out var cached))
                return cached;

            var template = RaceHelper.FindTemplate(culture, isFemale, race);
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

        /// <summary>
        /// Determines if the given character has a valid species combination.
        /// </summary>
        static bool IsValidSpeciesCombo(WCharacter wc)
        {
            if (wc == null)
                return true;

            if (!RaceHelper.HasAlternateSpecies())
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

            var valid = RaceHelper.GetValidRacesFor(wc.Culture, wc.IsFemale);
            if (valid.Count == 0)
                return true;

            return valid.Contains(wc.Race);
        }

        /// <summary>
        /// Shows a popup indicating that no valid model could be rendered.
        /// </summary>
        static void ShowNoValidModelPopup()
        {
            Inquiries.Popup(
                L.T("no_valid_model_title_species", "No Valid Model"),
                L.T(
                    "no_valid_model_body_species",
                    "This combination of gender, culture and species cannot be rendered.\n\nThe previous appearance has been restored."
                )
            );
        }

        /// <summary>
        /// Tries to apply an appearance change, restoring the previous appearance if invalid.
        /// </summary>
        public static bool TryApply(Func<bool> applyChange, WCharacter wc)
        {
            if (applyChange == null)
                return false;

            if (!RaceHelper.HasAlternateSpecies() || wc == null)
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

            if (!CanRender(wc.Culture, wc.IsFemale, wc.Race))
            {
                snap.Restore(wc);
                ShowNoValidModelPopup();
                return false;
            }

            if (IsValidSpeciesCombo(wc))
                return true;

            snap.Restore(wc);
            ShowNoValidModelPopup();
            return false;
        }
    }
}
