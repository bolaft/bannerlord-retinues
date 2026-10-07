using System;
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
        /// The current Bannerlord application version. Normally read from Parameters/Version.xml,
        /// but some installs (total conversions with their own launchers, partially validated or
        /// otherwise broken installs) have no readable version file, which reported 0.0.0 and
        /// flipped every version gate — most visibly rendering the whole editor upside down on
        /// game 1.4+, since the layout prefab is chosen by IsAtLeast14(). In that case the game
        /// generation is inferred from API markers instead.
        /// </summary>
        public static readonly ApplicationVersion Version = ReadVersion();

        private static ApplicationVersion ReadVersion()
        {
            try
            {
                var v = ApplicationVersion.FromParametersFile();
                if (v.Major > 0)
                    return v;
            }
            catch
            {
                // Missing or malformed Parameters/Version.xml; fall through to probing.
            }

            int minor;
            try
            {
                if (
                    typeof(TaleWorlds.CampaignSystem.MapEvents.MapEvent).GetMethod(
                        "GetBattleRewards",
                        System.Reflection.BindingFlags.Public
                            | System.Reflection.BindingFlags.Instance
                    ) == null
                )
                    minor = 4; // Removed in 1.4 (also matches 1.5+; only the >=1.4 gate matters).
                else if (
                    Type.GetType(
                        "TaleWorlds.CampaignSystem.Naval.Figurehead, TaleWorlds.CampaignSystem"
                    ) != null
                )
                    minor = 3; // Naval types shipped with 1.3.
                else
                    minor = 2;
            }
            catch
            {
                minor = 2;
            }

            Log.Warn(
                "Could not read the game version from Parameters/Version.xml; "
                    + $"probed the game API instead: 1.{minor}.x."
            );

            return new ApplicationVersion(ApplicationVersionType.Release, 1, minor, 0, 0);
        }

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
