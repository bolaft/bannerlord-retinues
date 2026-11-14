using System;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Settings.Controllers
{
    /// <summary>
    /// Controller for destructive purge actions.
    /// </summary>
    public sealed class PurgeController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Purge All                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Purges all custom troops from all party rosters, either by removing or replacing them.
        /// </summary>
        public static ControllerAction<object> PurgeAll { get; } =
            Action<object>("PurgeAll")
                .DefaultTooltip(
                    L.T("purge_all_tooltip", "Purge all custom troop data from the game")
                )
                .ExecuteWith(_ => PurgeAllImpl());

        private static void PurgeAllImpl()
        {
            if (Campaign.Current == null)
            {
                Inquiries.Popup(
                    L.T("purge_title", "Purge Custom Troops"),
                    L.T("purge_need_campaign_reason", "No running campaign. Load a save first.")
                );
                return;
            }

            Inquiries.Popup(
                title: L.T("purge_confirm_title", "Purge Custom Troops"),
                onConfirm: () => ShowPurgeModePopup(),
                description: L.T(
                    "purge_confirm_verified_body",
                    "This prepares this campaign for removing Retinues. It cleans custom troop references, including parties, prisoners, village recruits and quests, and disables Retinues troop generation for this save.\n\nTo play with Retinues again you will need your original save. Make a backup first. Continue?"
                ),
                confirmText: L.T("purge_continue", "Continue"),
                cancelText: GameTexts.FindText("str_cancel"),
                pauseGame: true
            );
        }

        private static void ShowPurgeModePopup()
        {
            Inquiries.Popup(
                title: L.T("purge_mode_title", "Purge Mode"),
                onChoice1: () => ExecutePurge(removeOnly: true),
                onChoice2: () => ExecutePurge(removeOnly: false),
                choice1Text: L.T("purge_mode_remove", "Remove"),
                choice2Text: L.T("purge_mode_replace", "Replace"),
                description: L.T(
                    "purge_mode_verified_body",
                    "Remove custom troops from army and prisoner rosters, or replace them with similar non-Retinues troops? Recruitment slots and other campaign references will be replaced in either mode. Troops from other installed mods are kept."
                ),
                pauseGame: true
            );
        }

        private static void ExecutePurge(bool removeOnly)
        {
            try
            {
                Safety.Uninstall.UninstallCleanup.Run(removeOnly);
                // Leave the editor so its old selection cannot be edited after the purge.
                if (Game.Current?.GameStateManager?.ActiveState is EditorGameState)
                    Game.Current.GameStateManager.PopState();
                Inquiries.Popup(
                    L.T("purge_verified_title", "Ready to Uninstall"),
                    L.T("purge_verified_body", "Custom troop references have been cleaned and checked, including village recruits and settlement rosters. Retinues troop generation is now disabled for this save.\n\nSave to a NEW slot, exit to desktop, then remove Retinues. Keep your original backup.")
                );
            }
            catch (Exception e)
            {
                Log.Exception(e, "Uninstall cleanup failed.");
                Inquiries.Popup(
                    L.T("purge_incomplete_title", "Cleanup Incomplete"),
                    L.T("purge_incomplete_body", "The cleanup could not be verified. Do not remove Retinues or overwrite your backup. Reload your pre-uninstall save and report the debug.log.\n\n{ERROR}")
                        .SetTextVariable("ERROR", e.Message)
                );
            }
        }
    }
}
