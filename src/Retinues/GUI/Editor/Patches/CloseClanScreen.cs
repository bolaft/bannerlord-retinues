using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

namespace Retinues.GUI.Editor.Patches
{
    [HarmonyPatch(typeof(ClanManagementVM), nameof(ClanManagementVM.ExecuteClose))]
    internal static class ClanManagementVM_Close_Patch
    {
        static void Prefix()
        {
            State.IsStudioMode = false;
        }
    }
}
