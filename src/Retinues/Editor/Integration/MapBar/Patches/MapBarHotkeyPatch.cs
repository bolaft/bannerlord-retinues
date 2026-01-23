using HarmonyLib;
using Retinues.Settings;
using Retinues.Utilities;
using SandBox.GauntletUI.Map;
using TaleWorlds.InputSystem;
#if BL13
using SandBox.View.Map.Navigation;
#else
using SandBox.View;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
#endif

namespace Retinues.Editor.Integration.MapBar.Patches
{
    /// <summary>
    /// Enables opening the troop editor via the R hotkey on the map bar.
    /// </summary>
#if BL13
    [HarmonyPatch(typeof(GauntletMapBarGlobalLayer), "HandlePanelSwitchingInput")]
#else
    [HarmonyPatch(typeof(GauntletMapBarGlobalLayer), "HandlePanelSwitching")]
#endif
    internal static class MapBarHotkeyPatch
    {
#if BL13
        [HarmonyPostfix]
        private static void Postfix(
            GauntletMapBarGlobalLayer __instance,
            InputContext inputContext,
            ref bool __result
        )
        {
            if (__result)
                return;

            if (!Configuration.EditorHotkey)
                return;

            // Runs under vanilla mapbar gating (focused layer + not focused on input),
            // so it will NOT trigger while typing in text fields.
            if (!inputContext.IsKeyReleased(InputKey.R))
                return;

            // BL13: map bar uses INavigationHandler, but the concrete type is SandBox.View.Map.Navigation.MapNavigationHandler.
            var handler =
                Reflection.GetFieldValue<object>(__instance, "_mapNavigationHandler")
                as MapNavigationHandler;
            var el =
                handler?.GetElement(TroopsNavigationElement.TroopsId) as MapNavigationElementBase;
            if (el == null)
                return;

            if (!el.Permission.IsAuthorized || el.IsActive)
                return;

            el.OpenView();
            __result = true;
        }
#else
        [HarmonyPostfix]
        private static void Postfix(GauntletMapBarGlobalLayer __instance)
        {
            if (!Settings.EditorHotkey)
                return;

            // Mirror vanilla gating in HandlePanelSwitching: only when the top GauntletLayer exists and isn't focused on input.
            var top = ScreenManager.TopScreen;
            var gauntletLayer = top?.FindLayer<GauntletLayer>();
            if (gauntletLayer?.Input == null)
                return;

            if (gauntletLayer.IsFocusedOnInput())
                return;

            var inputContext = gauntletLayer.Input;
            if (!inputContext.IsKeyReleased(InputKey.R))
                return;

            // BL12: there is no Navigation elements API. Just apply vanilla-like gating and open the editor.
            var handler = Reflection.GetFieldValue<MapNavigationHandler>(
                __instance,
                "_mapNavigationHandler"
            );
            if (handler == null)
                return;

            if (handler.IsNavigationLocked || handler.EscapeMenuActive)
                return;

            // Mirror key vanilla lock cases.
            if (PlayerEncounter.CurrentBattleSimulation != null)
                return;

            if (top is MapScreen mapScreen)
            {
                if (
                    mapScreen.IsInArmyManagement
                    || mapScreen.IsMarriageOfferPopupActive
                    || mapScreen.IsMapCheatsActive
                )
                    return;

                if (
                    mapScreen.EncyclopediaScreenManager != null
                    && mapScreen.EncyclopediaScreenManager.IsEncyclopediaOpen
                )
                    return;
            }

            static void OpenEditor() => EditorLauncher.Launch(EditorMode.Player);

            // Match vanilla: warn about unsaved changes on the currently open panel.
            if (top is IChangeableScreen changeable && changeable.AnyUnsavedChanges())
            {
                InformationManager.ShowInquiry(
                    changeable.CanChangesBeApplied()
                        ? GetUnsavedChangedInquiry(OpenEditor)
                        : GetUnapplicableChangedInquiry()
                );
            }
            else
            {
                SwitchToANewScreen(OpenEditor);
            }
        }

        /// <summary>
        /// Builds the vanilla-style "unsaved changes" inquiry (BL12 labels).
        /// </summary>
        private static InquiryData GetUnsavedChangedInquiry(System.Action openNewScreenAction)
        {
            return new InquiryData(
                string.Empty,
                GameTexts.FindText("str_unsaved_changes").ToString(),
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: GameTexts.FindText("str_apply").ToString(),
                negativeText: GameTexts.FindText("str_cancel").ToString(),
                affirmativeAction: () =>
                {
                    ApplyCurrentChanges();
                    SwitchToANewScreen(openNewScreenAction);
                },
                negativeAction: () => SwitchToANewScreen(openNewScreenAction)
            );
        }

        /// <summary>
        /// Builds the vanilla-style "unapplicable changes" inquiry (BL12 labels).
        /// </summary>
        private static InquiryData GetUnapplicableChangedInquiry()
        {
            return new InquiryData(
                string.Empty,
                GameTexts.FindText("str_unapplicable_changes").ToString(),
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: GameTexts.FindText("str_apply").ToString(),
                negativeText: GameTexts.FindText("str_cancel").ToString(),
                affirmativeAction: null,
                negativeAction: null
            );
        }

        /// <summary>
        /// Applies or resets changes on the current screen using the IChangeableScreen contract.
        /// </summary>
        private static void ApplyCurrentChanges()
        {
            if (
                ScreenManager.TopScreen is IChangeableScreen changeable
                && changeable.AnyUnsavedChanges()
            )
            {
                if (changeable.CanChangesBeApplied())
                    changeable.ApplyChanges();
                else
                    changeable.ResetChanges();
            }
        }

        /// <summary>
        /// Closes the current panel if needed, then opens the requested screen/state.
        /// </summary>
        private static void SwitchToANewScreen(System.Action openNewScreenAction)
        {
            if (!(ScreenManager.TopScreen is MapScreen))
                Game.Current?.GameStateManager?.PopState();

            openNewScreenAction?.Invoke();
        }
#endif
    }
}
