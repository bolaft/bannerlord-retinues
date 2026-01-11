using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.UI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Game.Retinues
{
    public partial class RetinuesBehavior
    {
        internal enum RetinueProgressSource
        {
            TournamentWon,
            QuestCompleted,
            OwnedFiefsDaily,
            OwnedWorkshopsDaily,
            WonBattleWithAllies,
        }

        /// <summary>
        /// Ensures the player has a default retinue and unlocks their culture's retinue.
        /// </summary>
        private void EnsureDefaultRetinueForPlayer()
        {
            if (!Settings.EnableRetinues)
                return;

            var clan = Player.Clan;
            if (clan?.Base == null)
                return;

            if (clan.RosterRetinues.IsEmpty())
            {
                var name = L.T("retinue_default_name", "{CLAN} House Guard")
                    .SetTextVariable("CLAN", clan.Name)
                    .ToString();

                EnsureDefaultRetinue(clan, name, notifyUnlocks: false);
            }

            // No popup for base default culture unlock.
            UnlockCulture(Player.Clan.Culture, showPopup: false);
        }

        /// <summary>
        /// Adds progress toward retinue unlock for the given culture.
        /// </summary>
        private void AddProgress(
            WCulture culture,
            int amount,
            RetinueProgressSource source,
            bool showProgressMessage
        )
        {
            if (culture?.Base == null)
                return;

            if (amount <= 0)
                return;

            var id = culture.Base.StringId;
            if (string.IsNullOrEmpty(id))
                return;

            if (IsUnlocked(id))
                return;

            // Apply global multiplier.
            amount = (int)(amount * Settings.RetinueUnlockSpeed);
            if (amount <= 0)
                return;

            var before = GetProgress(id);
            var after = before + amount;

            if (after >= UnlockProgressTarget)
                after = UnlockProgressTarget;

            if (after == before)
                return;

            SetProgress(id, after);

            if (showProgressMessage)
                NotifyProgress(culture, source, after - before, after);

            if (after >= UnlockProgressTarget)
                UnlockCulture(culture, showPopup: true);
        }

        /// <summary>
        /// Unlocks the retinue for the given culture.
        /// </summary>
        private void UnlockCulture(WCulture culture, bool showPopup)
        {
            if (culture?.Base == null)
                return;

            var id = culture.Base.StringId;
            if (string.IsNullOrEmpty(id))
                return;

            if (IsUnlocked(id))
                return;

            _unlockedCultureIds.Add(id);
            SetProgress(id, UnlockProgressTarget);

            // Create the retinue immediately for newly unlocked cultures.
            // (Player culture already has the default one.)
            WCharacter created = null;
            if (showPopup)
                created = EnsureRetinueExistsForCulture(culture);

            Log.Info($"Unlocked retinue for culture '{culture.Name}'.");

            if (showPopup && created?.Base != null)
                NotifyUnlockedPopup(created);
        }

        /// <summary>
        /// Checks if the retinue for the given culture is unlocked.
        /// </summary>
        private bool IsUnlocked(string cultureId)
        {
            if (string.IsNullOrEmpty(cultureId))
                return false;

            for (int i = 0; i < _unlockedCultureIds.Count; i++)
            {
                if (_unlockedCultureIds[i] == cultureId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the current progress toward retinue unlock for the given culture.
        /// </summary>
        private int GetProgress(string cultureId)
        {
            if (string.IsNullOrEmpty(cultureId))
                return 0;

            NormalizeProgressLists();

            for (int i = 0; i < _progressCultureIds.Count; i++)
            {
                if (_progressCultureIds[i] == cultureId)
                    return _progressValues[i];
            }

            return 0;
        }

        /// <summary>
        /// Sets the current progress toward retinue unlock for the given culture.
        /// </summary>
        private void SetProgress(string cultureId, int value)
        {
            if (string.IsNullOrEmpty(cultureId))
                return;

            NormalizeProgressLists();

            value = MathF.Max(0, value);

            for (int i = 0; i < _progressCultureIds.Count; i++)
            {
                if (_progressCultureIds[i] == cultureId)
                {
                    _progressValues[i] = value;
                    return;
                }
            }

            _progressCultureIds.Add(cultureId);
            _progressValues.Add(value);
        }

        /// <summary>
        /// Normalizes the progress and unlock lists to ensure data integrity.
        /// </summary>
        private void NormalizeProgressLists()
        {
            _progressCultureIds ??= [];
            _progressValues ??= [];
            _unlockedCultureIds ??= [];

            while (_progressValues.Count < _progressCultureIds.Count)
                _progressValues.Add(0);

            while (_progressCultureIds.Count < _progressValues.Count)
                _progressValues.RemoveAt(_progressValues.Count - 1);

            for (int i = _progressCultureIds.Count - 1; i >= 0; i--)
            {
                var id = _progressCultureIds[i];
                if (!string.IsNullOrEmpty(id))
                    continue;

                _progressCultureIds.RemoveAt(i);
                _progressValues.RemoveAt(i);
            }
        }
    }
}
