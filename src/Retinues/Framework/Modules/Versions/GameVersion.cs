using TaleWorlds.Library;

namespace Retinues.Framework.Modules.Versions
{
    /// <summary>
    /// Utility for querying the running Bannerlord engine version at runtime.
    /// </summary>
    public static class GameVersion
    {
        private static ApplicationVersion? _cached;

        // Resolve the engine version, but NEVER cache an Empty result: FromParametersFile can return
        // Empty if called before the parameters file is ready during an early load, and caching that
        // would leave IsAtLeast14() stuck false for the whole session (which, among other things,
        // dropped troops' Mariner skill). Retry until we get a real version.
        private static ApplicationVersion Current
        {
            get
            {
                if (_cached == null || _cached.Value == ApplicationVersion.Empty)
                    _cached = ApplicationVersion.FromParametersFile();

                return _cached.Value;
            }
        }

        /// <summary>
        /// Returns true if running on Bannerlord 1.4.x or later.
        /// BL14 fixed the StackLayout vertical direction bug present in BL13 and earlier,
        /// so UI prefabs built for BL13 produce an upside-down layout on BL14 and vice-versa.
        /// </summary>
        public static bool IsAtLeast14() =>
            Current.Major > 1 || (Current.Major == 1 && Current.Minor >= 4);
    }
}
