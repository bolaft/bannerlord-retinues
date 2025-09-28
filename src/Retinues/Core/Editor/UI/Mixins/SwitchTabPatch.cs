
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public static class ClanTabBlocker
{
    public static volatile bool BlockTabSwitch = true;
}

[HarmonyPatch(typeof(ClanManagementVM))]
static class ClanManagementVM_TabGuards
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ClanManagementVM.SelectPreviousCategory))]
    static bool Prev_Prefix() => !ClanTabBlocker.BlockTabSwitch;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ClanManagementVM.SelectNextCategory))]
    static bool Next_Prefix() => !ClanTabBlocker.BlockTabSwitch;
}
