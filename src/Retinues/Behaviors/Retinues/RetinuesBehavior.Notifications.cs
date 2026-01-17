using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.GUI.Editor;
using Retinues.GUI.Services;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Behaviors.Retinues
{
    public partial class RetinuesBehavior
    {
        /// <summary>
        /// Notifies the player of progress made toward retinue unlock.
        /// </summary>
        private void NotifyProgress(
            WCulture culture,
            RetinueProgressSource source,
            int deltaPoints,
            int totalPoints
        )
        {
            if (culture?.Base == null)
                return;

            if (deltaPoints <= 0)
                return;

            var sourceText = GetProgressSourceText(source);
            var deltaPct = PercentOfTarget(deltaPoints);
            var totalPct = PercentOfTarget(totalPoints);

            // "Tournament won: +5% progress towards unlocking Vlandia retinue. Total progress: 35%."
            var text = L.T(
                    "retinue_unlock_progress",
                    "{SOURCE}: +{DELTA}% progress towards unlocking {CULTURE} retinue. Total progress: {TOTAL}%."
                )
                .SetTextVariable("SOURCE", sourceText)
                .SetTextVariable("DELTA", deltaPct)
                .SetTextVariable("CULTURE", culture.Name)
                .SetTextVariable("TOTAL", totalPct)
                .ToString();

            Notifications.Message(text);
        }

        /// <summary>
        /// Notifies the player of a newly unlocked retinue via popup.
        /// </summary>
        private void NotifyUnlockedPopup(WCharacter retinue)
        {
            var title = L.T("retinue_unlock_popup_title", "New Retinue Acquired");
            var body = L.T(
                    "retinue_unlock_popup_text",
                    "You can now hire your newly acquired {RETINUE}."
                )
                .SetTextVariable("RETINUE", retinue.Name);

            var ok = GameTexts.FindText("str_ok");
            var go = L.T("go_to_editor", "Go to editor");

            Inquiries.Popup(
                title,
                onChoice1: () => { }, // OK
                onChoice2: () =>
                    EditorLauncher.Launch(
                        EditorLaunchArgs.Player(faction: Player.Clan, character: retinue)
                    ),
                choice1Text: ok,
                choice2Text: go,
                description: body,
                pauseGame: true,
                delayUntilOnWorldMap: true
            );
        }

        /// <summary>
        /// Calculates the percentage of progress toward the unlock target.
        /// </summary>
        private static int PercentOfTarget(int progress)
        {
            progress = (int)MathF.Clamp(progress, 0, UnlockProgressTarget);

            var pct = MathF.Round(progress * 100f / UnlockProgressTarget);
            if (pct > 100)
                pct = 100;

            return pct;
        }

        /// <summary>
        /// Gets the localized text for the given progress source.
        /// </summary>
        private string GetProgressSourceText(RetinueProgressSource source)
        {
            return source switch
            {
                RetinueProgressSource.TournamentWon => L.T(
                        "retinue_progress_source_tournament",
                        "Tournament won"
                    )
                    .ToString(),
                RetinueProgressSource.QuestCompleted => L.T(
                        "retinue_progress_source_quest",
                        "Quest completed"
                    )
                    .ToString(),
                RetinueProgressSource.OwnedFiefsDaily => L.T(
                        "retinue_progress_source_fiefs",
                        "Fiefs owned"
                    )
                    .ToString(),
                RetinueProgressSource.OwnedWorkshopsDaily => L.T(
                        "retinue_progress_source_workshops",
                        "Workshops owned"
                    )
                    .ToString(),
                RetinueProgressSource.WonBattleWithAllies => L.T(
                        "retinue_progress_source_allies",
                        "Won battle with allies"
                    )
                    .ToString(),
                _ => L.T("retinue_progress_source_generic", "Progress").ToString(),
            };
        }
    }
}
