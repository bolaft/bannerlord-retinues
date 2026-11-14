using Retinues.Behaviors.Volunteers;
using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Safety.Uninstall
{
    internal static class UninstallHooks
    {
        internal static void Log(string message) => Utilities.Log.Info(message);
        internal static void RestoreTemporaryReferences()
        {
            PlayerVolunteerSwapState.RestoreIfActive();
            Behaviors.Retinues.Patches.RetinueDynamicUpgradePatch.RestoreAll();
        }
        internal static void DisableCustomTroops()
        {
            foreach (var troop in WCharacter.All)
                if (troop.IsCustom && !troop.IsHero) troop.IsActiveStub = false;
        }
    }
}
