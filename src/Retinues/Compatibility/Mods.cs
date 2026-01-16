using Retinues.Framework.Runtime;

namespace Retinues.Modules
{
    [SafeClass]
    public static class Mods
    {
        /* ━━━━━━━ Official ━━━━━━━ */

        public static ModuleManager.ModuleInfo NavalDLC => ModuleManager.GetModule("NavalDLC");

        /* ━━━━━━━ Community ━━━━━━ */

        public static ModuleManager.ModuleInfo Shokuho => ModuleManager.GetModule("Shokuho");

        public static ModuleManager.ModuleInfo BanditMilitias =>
            ModuleManager.GetModule("BanditMilitias");

        public static ModuleManager.ModuleInfo T7TroopUnlocker =>
            ModuleManager.GetModule("T7TroopUnlocker");
    }
}
