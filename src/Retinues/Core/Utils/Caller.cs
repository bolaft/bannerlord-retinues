// Retinues.Core/Utils/Caller.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Retinues.Core.Utils
{
    public static class Caller
    {
        // What we return when you want details
        public sealed class Info
        {
            public MethodBase Method { get; set; }
            public Type DeclaringType { get; set; }
            public string AssemblyName { get; set; }
            public string Namespace { get; set; }
            public string TypeName { get; set; }
            public string MethodName { get; set; }
            public string Label { get; set; } // e.g. "Type.Method" or "Namespace.Type.Method"
            public string FileName { get; set; } // may be null if no PDB / not requested
            public int Line { get; set; } // 0 if unknown
        }

        // Default filters to skip noise
        private static readonly string[] _skipNamespaces =
        {
            "System.",
            "Microsoft.",
            "HarmonyLib.",
            "TaleWorlds.Engine",
            "TaleWorlds.Library",
            "TaleWorlds.DotNet",
            "Retinues.Core.Utils", // skip utils (Log/Caller) by default
        };

        private static readonly HashSet<string> _skipTypes = new(StringComparer.Ordinal)
        {
            "Retinues.Core.Utils.Log",
            "Retinues.Core.Utils.Caller",
        };

        // Find the first meaningful caller on the stack.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Info Get(
            int skip = 0,
            int maxFrames = 24,
            bool includeFileInfo = false,
            bool includeNamespaceInLabel = false,
            Func<MethodBase, bool> extraSkipPredicate = null
        )
        {
            try
            {
                // skip: 1 (this method) + optional user skip
                var st = new StackTrace(skipFrames: 1 + skip, fNeedFileInfo: includeFileInfo);
                var count = Math.Min(st.FrameCount, Math.Max(4, maxFrames));

                for (int i = 0; i < count; i++)
                {
                    var frame = st.GetFrame(i);
                    var m = frame?.GetMethod();
                    if (m == null)
                        continue;

                    var t = m.DeclaringType;
                    if (t == null)
                        continue;

                    var fullType = t.FullName ?? t.Name ?? "";
                    // Skip common noise
                    if (StartsWithAny(fullType, _skipNamespaces))
                        continue;
                    if (_skipTypes.Contains(fullType))
                        continue;
                    if (m.Name is "Invoke" or "InvokeImpl")
                        continue; // delegate wrappers
                    if (extraSkipPredicate != null && extraSkipPredicate(m))
                        continue;

                    // Build label
                    var typeName = t.Name;
                    var methodName = NormalizeMethodName(m);
                    var ns = t.Namespace ?? string.Empty;

                    var label =
                        includeNamespaceInLabel && !string.IsNullOrEmpty(ns)
                            ? $"{ns}.{typeName}.{methodName}"
                            : $"{typeName}.{methodName}";

                    string file = null;
                    int line = 0;
                    if (includeFileInfo)
                    {
                        file = frame.GetFileName();
                        line = frame.GetFileLineNumber();
                    }

                    return new Info
                    {
                        Method = m,
                        DeclaringType = t,
                        AssemblyName = t.Assembly?.GetName()?.Name,
                        Namespace = ns,
                        TypeName = typeName,
                        MethodName = methodName,
                        Label = label,
                        FileName = file,
                        Line = line,
                    };
                }
            }
            catch
            {
                // fallthrough
            }

            // Fallback if nothing matched
            return new Info { Label = "<unknown>" };
        }

        // Convenience: get just a label like "Type.Method" (or with namespace).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetLabel(
            int skip = 0,
            bool includeNamespace = false,
            int maxFrames = 24
        )
        {
            return Get(
                    skip: skip,
                    maxFrames: maxFrames,
                    includeNamespaceInLabel: includeNamespace
                )?.Label ?? "<unknown>";
        }

        // Check whether the current call site matches a blacklist. If any entry equals or is contained in the label, returns true.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsBlocked(
            IReadOnlyCollection<string> blacklist,
            StringComparison cmp = StringComparison.Ordinal,
            bool allowSubstring = true,
            int skip = 0
        )
        {
            if (blacklist == null || blacklist.Count == 0)
                return false;
            var label = GetLabel(skip: skip + 1); // +1 to skip this helper
            foreach (var entry in blacklist)
            {
                if (string.Equals(label, entry, cmp))
                    return true;
                if (allowSubstring && label.IndexOf(entry, cmp) >= 0)
                    return true;
            }
            return false;
        }

        private static bool StartsWithAny(string s, string[] prefixes)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            for (int i = 0; i < prefixes.Length; i++)
                if (s.StartsWith(prefixes[i], StringComparison.Ordinal))
                    return true;
            return false;
        }

        private static string NormalizeMethodName(MethodBase m)
        {
            var n = m.Name;
            if (n is ".ctor" or ".cctor")
                return m.DeclaringType?.Name ?? n;
            return n;
        }
    }
}
