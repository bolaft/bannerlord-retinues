using Retinues.Utilities;

namespace Retinues.Module
{
    [SafeClass]
    public static class Mods
    {
        // Official
        public static ModuleManager.ModuleInfo NavalDLC => ModuleManager.GetModule("NavalDLC");

        // Community
        public static ModuleManager.ModuleInfo T7TroopUnlocker =>
            ModuleManager.GetModule("T7TroopUnlocker");
    }
}
