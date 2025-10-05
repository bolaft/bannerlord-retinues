using TaleWorlds.Library;

namespace Retinues.Core.Utils
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
    }
}
