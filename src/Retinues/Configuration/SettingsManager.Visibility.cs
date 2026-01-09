using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Retinues.Utilities;

namespace Retinues.Configuration
{
    public static partial class SettingsManager
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal static bool IsVisibleInMCM(string key)
        {
            DiscoverOptions();

            if (!_byKey.TryGetValue(key, out var opt) || opt == null)
                return true;

            return IsVisibleInMCM(opt, stack: null);
        }

        private static bool IsVisibleInMCM(IOption opt, HashSet<string> stack)
        {
            if (opt == null)
                return true;

            var dep = opt.DependsOn;
            if (dep == null)
                return true;

            stack ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Break cycles gracefully
            var k = opt.Key ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(k) && !stack.Add(k))
                return true;

            // If the dependency itself is not visible, this option is not visible
            if (!IsVisibleInMCM(dep, stack))
                return false;

            try
            {
                //If DependsOnValue is set, gate on equality for any dependency type.
                if (opt.DependsOnValue != null)
                {
                    object current = dep.GetObject();

                    // NEW: allow multiple acceptable values via IEnumerable (but not string)
                    if (opt.DependsOnValue is IEnumerable many && opt.DependsOnValue is not string)
                    {
                        foreach (var raw in many)
                        {
                            object expected = CoerceToType(raw, dep.Type);
                            if (Equals(current, expected))
                                return true;
                        }

                        return false;
                    }

                    // Old: single acceptable value
                    object single = CoerceToType(opt.DependsOnValue, dep.Type);
                    return Equals(current, single);
                }

                // Old behavior: only hide when dependency value is not true
                if (dep.Type != typeof(bool))
                    return true;

                return Convert.ToBoolean(dep.GetObject(), CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                Log.Exception(e, "DependsOn visibility check failed.");
                return true;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(k))
                    stack.Remove(k);
            }
        }

        private static object CoerceToType(object value, Type targetType)
        {
            if (targetType == null)
                return value;

            var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value == null)
                return null;

            if (t.IsInstanceOfType(value))
                return value;

            if (t.IsEnum)
            {
                if (value is string s)
                    return Enum.Parse(t, s, ignoreCase: true);

                var underlying = Enum.GetUnderlyingType(t);
                var num = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
                return Enum.ToObject(t, num);
            }

            return Convert.ChangeType(value, t, CultureInfo.InvariantCulture);
        }
    }
}
