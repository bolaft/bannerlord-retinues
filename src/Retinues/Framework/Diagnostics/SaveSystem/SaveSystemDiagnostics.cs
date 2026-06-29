using Retinues.Utilities;

namespace Retinues.Framework.Diagnostics.SaveSystem
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                 Save System Diagnostics                //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Diagnostics helpers for save/load operations.
    /// </summary>
    internal static class SaveSystemDiagnostics
    {
#if DEBUG
        // Set by the DEBUG-only save/load exception patches (ExceptionPatches) for inspection while
        // debugging a load crash. Not compiled into Release, where nothing assigns it.
        public static string LastSaveName;
#endif
        public static string LastLoadContextException;

        /// <summary>
        /// Record and log the textual representation of a LoadContext exception.
        /// </summary>
        public static void ReportLoadContextException(string text)
        {
            LastLoadContextException = text;

            Log.Error("SaveLoad: LoadContext.Load failed:\n" + text);
        }
    }
}
