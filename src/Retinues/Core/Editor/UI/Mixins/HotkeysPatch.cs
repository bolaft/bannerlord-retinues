
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.InputSystem;

public static class HotkeyBlocker
{
    public static volatile bool BlockHotkeys = true;
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

public static class ClanHotkeyGate
{
    // true while your ClanTroopScreen mixin is active
    public static volatile bool Active;
    // set to true if you want to require Shift+L instead of plain L
    public static bool RequireShift = true;
    public static bool Matches(InputKey key)
    {
        if (!Active) return false;
        if (key != InputKey.L) return false; // only the L key
        if (!RequireShift) return true;

        // Require either Shift key to be down
        return !(Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift));
        // ^ returns true when we should block plain L
    }
}

[HarmonyPatch(typeof(Input))]
static class Input_LKey_Blockers
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Input.IsKeyDown), typeof(InputKey))]
    static void IsKeyDown_Postfix(InputKey key, ref bool __result)
    {
        if (__result && ClanHotkeyGate.Matches(key))
            __result = false; // treat L as not held
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Input.IsKeyPressed), typeof(InputKey))]
    static void IsKeyPressed_Postfix(InputKey key, ref bool __result)
    {
        if (__result && ClanHotkeyGate.Matches(key))
            __result = false; // block the “just pressed” tick
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Input.IsKeyReleased), typeof(InputKey))]
    static void IsKeyReleased_Postfix(InputKey key, ref bool __result)
    {
        if (__result && ClanHotkeyGate.Matches(key))
            __result = false; // block the “just released” tick
    }
}