using TaleWorlds.Localization;

namespace Retinues.Core.Utils
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                  Localization Helpers                  //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Localization helpers for mod text.
    /// Provides methods to create TextObject with fallback and to get localized strings.
    /// </summary>
    public static class L
    {
        /// <summary>
        /// Returns a TextObject for the given id and fallback string.
        /// </summary>
        public static TextObject T(string id, string fallback) => new($"{{=ret_{id}}}{fallback}");

        /// <summary>
        /// Returns the localized string for the given id and fallback.
        /// </summary>
        public static string S(string id, string fallback) => T(id, fallback).ToString();
    }
}
