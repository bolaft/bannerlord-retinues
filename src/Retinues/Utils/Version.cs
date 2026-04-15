using TaleWorlds.Library;

namespace Retinues.Utils
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                   Bannerlord Version                   //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Utility for querying the running Bannerlord version.
    /// Used for version gates and compatibility checks.
    /// </summary>
    public static class BannerlordVersion
    {
        /// <summary>
        /// The current Bannerlord application version.
        /// </summary>
        public static readonly ApplicationVersion Version = ApplicationVersion.FromParametersFile();

        /// <summary>
        /// Returns true if running on Bannerlord 1.2.x.
        /// </summary>
        public static bool Is12()
        {
            return Version.Major == 1 && Version.Minor == 2;
        }

        /// <summary>
        /// Returns true if running on Bannerlord 1.3.x.
        /// </summary>
        public static bool Is13()
        {
            return Version.Major == 1 && Version.Minor == 3;
        }

        /// <summary>
        /// Returns true if running on Bannerlord 1.4.x or later.
        /// BL14 fixed the StackLayout vertical direction bug that was present in BL13 and earlier,
        /// so UI prefabs need different LayoutMethod values starting from this version.
        /// </summary>
        public static bool IsAtLeast14()
        {
            return Version.Major > 1 || (Version.Major == 1 && Version.Minor >= 4);
        }
    }
}
