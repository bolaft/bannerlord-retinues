using TaleWorlds.Library;

namespace Retinues.Core.Utils
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                   Bannerlord Version                   //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    public static class BannerlordVersion
    {
        public static readonly ApplicationVersion Version = ApplicationVersion.FromParametersFile();

        // quick version gate: only handle on 1.2.x; on other versions let vanilla run
        public static bool Is12()
        {
            return Version.Major == 1 && Version.Minor == 2;
        }

        public static bool Is13()
        {
            return Version.Major == 1 && Version.Minor == 3;
        }
    }
}
