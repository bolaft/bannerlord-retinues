using Retinues.Utilities;

namespace Retinues.Framework.Diagnostics.SaveSystem
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                 Save System Diagnostics                //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    internal static class SaveSystemDiagnostics
    {
        public static string LastSaveName;
        public static string LastLoadContextException;

        public static void ReportLoadContextException(string text)
        {
            LastLoadContextException = text;

            Log.Error("SaveLoad: LoadContext.Load failed:\n" + text);
        }
    }
}
