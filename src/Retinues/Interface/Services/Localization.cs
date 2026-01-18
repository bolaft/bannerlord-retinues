using System;
using TaleWorlds.Localization;

namespace Retinues.Interface.Services
{
    /// <summary>
    /// Localization helpers.
    /// </summary>
    public static partial class L
    {
        public static TextObject T(string id, string fallback) => new($"{{=ret_{id}}}{fallback}");

        public static string S(string id, string fallback) => T(id, fallback).ToString();

        public static Func<string> F(string id, string fallback) => () => S(id, fallback);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Text variables                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns a TextObject with text variables applied.
        /// </summary>
        public static TextObject TV(
            string id,
            string fallback,
            params (string Key, object Value)[] vars
        )
        {
            var t = T(id, fallback);

            if (vars != null)
            {
                for (int i = 0; i < vars.Length; i++)
                {
                    var k = vars[i].Key;
                    if (string.IsNullOrWhiteSpace(k))
                        continue;

                    var v = vars[i].Value;
                    if (v == null)
                        continue;

                    if (v is TextObject to)
                        t.SetTextVariable(k, to);
                    else if (v is string s)
                        t.SetTextVariable(k, s);
                    else if (v is int iv)
                        t.SetTextVariable(k, iv);
                    else if (v is float fv)
                        t.SetTextVariable(k, fv);
                    else if (v is double dv)
                        t.SetTextVariable(k, (float)dv); // or dv.ToString("F2") if you prefer
                    else if (v is bool bv)
                        t.SetTextVariable(k, bv ? "true" : "false");
                    else
                        t.SetTextVariable(k, v.ToString());
                }
            }

            return t;
        }

        /// <summary>
        /// Returns the localized string with text variables applied.
        /// </summary>
        public static string S(string id, string fallback, params (string Key, object Value)[] vars)
        {
            return TV(id, fallback, vars).ToString();
        }

        /// <summary>
        /// Returns a function that provides the localized string with text variables applied.
        /// </summary>
        public static Func<string> F(
            string id,
            string fallback,
            params (string Key, object Value)[] vars
        )
        {
            return () => S(id, fallback, vars);
        }

        /// <summary>
        /// Advanced form: caller mutates the TextObject (SetTextVariable, SetTextVariable, etc).
        /// </summary>
        public static Func<string> F(string id, string fallback, Action<TextObject> configure)
        {
            return () =>
            {
                var t = T(id, fallback);
                configure?.Invoke(t);
                return t.ToString();
            };
        }
    }
}
