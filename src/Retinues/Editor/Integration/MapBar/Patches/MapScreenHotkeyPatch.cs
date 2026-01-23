using System.Linq;
using HarmonyLib;
using Retinues.Domain;
using Retinues.Settings;
using Retinues.Utilities;
using SandBox.View.Map;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;
#if BL13
using SandBox.View.Map.Navigation;
#else
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.Library;
using SandBox.View;
#endif

namespace Retinues.Editor.Integration.MapBar.Patches
{
    /// <summary>
    /// Enables opening the troop editor via the R hotkey on the map screen.
    /// </summary>
    [HarmonyPatch(typeof(MapScreen), "TickNavigationInput")]
    internal static class MapScreenTroopsHotkeyPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MapScreen __instance, ref bool __result)
        {
            if (__result)
                return;

            if (!Configuration.EditorHotkey)
                return;

            var sceneLayer = __instance.SceneLayer;
            if (sceneLayer?.Input == null)
                return;

            // Match vanilla gating
            if (sceneLayer.Input.IsShiftDown() || sceneLayer.Input.IsControlDown())
                return;

            // Critical: prevents triggering while typing in Gauntlet textboxes (save name, search fields, etc.)
            if (ScreenManager.FocusedLayer != sceneLayer)
                return;

            if (!sceneLayer.Input.IsKeyPressed(InputKey.R))
                return;

            if (Player.Clan.Troops.Count() == 0)
                return;

#if BL13
            var handler = Reflection.GetFieldValue<MapNavigationHandler>(
                __instance,
                "_navigationHandler"
            );

            if (
                handler?.GetElement(TroopsNavigationElement.TroopsId)
                is not MapNavigationElementBase el
            )
                return;

            if (!el.Permission.IsAuthorized || el.IsActive)
                return;

            el.OpenView();
            __instance.MapCursor?.SetVisible(false);

            __result = true;
#else
            // BL12: there are no map navigation elements; just open the editor directly,
            // while preserving "unsaved changes" behavior similar to vanilla.
            if (PlayerEncounter.CurrentBattleSimulation != null)
                return;

            static void OpenEditor() => EditorLauncher.Launch(EditorMode.Player);

            if (
                ScreenManager.TopScreen is IChangeableScreen changeable
                && changeable.AnyUnsavedChanges()
            )
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

            var cursor = Reflection.GetFieldValue<MapCursor>(__instance, "_mapCursor");
            cursor?.SetVisible(false);
            __result = true;
#endif
        }

#if BL12
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
