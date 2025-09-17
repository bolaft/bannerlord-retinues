using TaleWorlds.Localization;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
//                  Localization Helpers                  //
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

public static class L
{
    public static TextObject T(string id, string fallback) => new($"{{=ret_{id}}}{fallback}");

    public static string S(string id, string fallback) => T(id, fallback).ToString();
}
