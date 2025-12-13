using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;

namespace Retinues.GUI.Prefabs.ClanScreen.Patches
{
    [HarmonyPatch(typeof(EditableTextWidget))]
    static class EditableTextWidget_HotkeyBlocker
    {
        // Called when any EditableTextWidget gains focus (user clicks into a text box).
        [HarmonyPostfix]
        [HarmonyPatch("OnGainFocus")]
        static void OnGainFocus_Postfix()
        {
            HotkeyBlocker.SetBlocked(true);
        }

        // Called when it loses focus (user clicks away / focus moves somewhere else).
        [HarmonyPostfix]
        [HarmonyPatch("OnLoseFocus")]
        static void OnLoseFocus_Postfix()
        {
            HotkeyBlocker.SetBlocked(false);
        }
    }

    /// <summary>
    /// Harmony patches and helpers for blocking vanilla clan management custom inputs are focused.
    /// </summary>
    public static class HotkeyBlocker
    {
        // Start with hotkeys allowed; we will set this from the filter input.
        public static volatile bool BlockHotkeys = false;

        public static void SetBlocked(bool value)
        {
            BlockHotkeys = value;
        }
    }

    [HarmonyPatch(typeof(ClanManagementVM))]
    static class ClanManagementVM_HotkeyGuards
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ClanManagementVM.SelectPreviousCategory))]
        static bool Prev_Prefix() => !HotkeyBlocker.BlockHotkeys;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ClanManagementVM.SelectNextCategory))]
        static bool Next_Prefix() => !HotkeyBlocker.BlockHotkeys;
    }

    [HarmonyPatch(typeof(Input))]
    static class Input_LKey_Blockers
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Input.IsKeyDown), typeof(InputKey))]
        static void IsKeyDown_Postfix(InputKey key, ref bool __result)
        {
            if (__result && key == InputKey.L)
                __result = false; // treat L as not held
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Input.IsKeyPressed), typeof(InputKey))]
        static void IsKeyPressed_Postfix(InputKey key, ref bool __result)
        {
            if (__result && key == InputKey.L)
                __result = false; // block the "just pressed" tick
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Input.IsKeyReleased), typeof(InputKey))]
        static void IsKeyReleased_Postfix(InputKey key, ref bool __result)
        {
            if (__result && key == InputKey.L)
                __result = false; // block the "just released" tick
        }
    }
}
