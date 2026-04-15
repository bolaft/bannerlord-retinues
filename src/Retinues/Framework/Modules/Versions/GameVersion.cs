using TaleWorlds.Library;

namespace Retinues.Framework.Modules.Versions
{
    /// <summary>
    /// Utility for querying the running Bannerlord engine version at runtime.
    /// </summary>
    public static class GameVersion
    {
        private static ApplicationVersion? _cached;

        private static ApplicationVersion Current =>
            _cached ??= ApplicationVersion.FromParametersFile();

        /// <summary>
        /// Returns true if running on Bannerlord 1.4.x or later.
        /// BL14 fixed the StackLayout vertical direction bug present in BL13 and earlier,
        /// so UI prefabs built for BL13 produce an upside-down layout on BL14 and vice-versa.
        /// </summary>
        public static bool IsAtLeast14() =>
            Current.Major > 1 || (Current.Major == 1 && Current.Minor >= 4);
    }
}
