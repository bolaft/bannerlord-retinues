
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

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
