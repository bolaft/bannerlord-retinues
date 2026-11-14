using Retinues.Game.Wrappers;
using Retinues.Features.Volunteers.Patches;

namespace Retinues.Safety.Uninstall
{
    internal static class UninstallHooks
    {
        internal static void Log(string message) => Utils.Log.Info(message);
        internal static void RestoreTemporaryReferences() => VolunteerSwapForPlayer.RestoreSnapshot();
        internal static void DisableCustomTroops() => WCharacter.ActiveStubIds.Clear();
    }
}
