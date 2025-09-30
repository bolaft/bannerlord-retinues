
public static class Version
{
        // quick version gate: only handle on 1.2.x; on other versions let vanilla run
        public static bool Is12()
        {
            try
            {
                var v = TaleWorlds.Library.ApplicationVersion.FromParametersFile();
                return v.Major == 1 && v.Minor == 2;
            }
            catch { return true; } // fail-safe: treat as 1.2 if version lookup fails
        }
}