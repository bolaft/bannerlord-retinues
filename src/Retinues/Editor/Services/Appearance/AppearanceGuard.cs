using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Helpers;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;

namespace Retinues.Editor.Services.Appearance
{
    public static class AppearanceGuard
    {
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

        static readonly Dictionary<string, bool> _renderableCache = [];

        static string RenderKey(WCulture culture, bool isFemale, int race)
        {
            var c = culture?.StringId ?? "null";
            return $"{c}|{(isFemale ? "F" : "M")}|{race}";
        }

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

        public static bool TryApply(Func<bool> applyChange) =>
            TryApply(applyChange, EditorState.Instance.Character?.Editable as WCharacter);

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
