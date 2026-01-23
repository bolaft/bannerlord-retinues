using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Retinues.Utilities
{
    /// <summary>
    /// Utility for inspecting the call stack and extracting caller information.
    /// Used for logging, debugging, and filtering out noise from stack traces.
    /// </summary>
    public static class Caller
    {
        /// <summary>
        /// Detailed information about a method call site.
        /// </summary>
        public sealed class Info
        {
            public MethodBase Method { get; set; }
            public Type DeclaringType { get; set; }
            public string AssemblyName { get; set; }
            public string Namespace { get; set; }
            public string TypeName { get; set; }
            public string MethodName { get; set; }
            public string Label { get; set; }
            public string FileName { get; set; }
            public int Line { get; set; }
        }

        // Default filters to skip noise
        private static readonly string[] _skipNamespaces =
        [
            "System.",
            "Microsoft.",
            "HarmonyLib.",
            "TaleWorlds.Engine",
            "TaleWorlds.Library",
            "TaleWorlds.DotNet",
            "Retinues.Utilities",
        ];

        private static readonly HashSet<string> _skipTypes = new(StringComparer.Ordinal)
        {
            "Retinues.Utilities.Log",
            "Retinues.Utilities.Caller",
        };

        /// <summary>
        /// Finds the first meaningful caller on the stack, skipping known noise.
        /// </summary>
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
                var st = new StackTrace(skipFrames: 1 + skip, fNeedFileInfo: includeFileInfo);
                var count = Math.Min(st.FrameCount, Math.Max(4, maxFrames));

                for (int i = 0; i < count; i++)
                {
                    var frame = st.GetFrame(i);
                    var m = frame?.GetMethod();
                    if (m == null)
                        continue;

                    ResolveToUserFrame(m, out var rt, out var rm);
                    if (rt == null)
                        continue;

                    // If we still can't escape compiler-gen, keep walking.
                    // (Usually rt becomes the outer declaring type; this is a final safety net.)
                    if (IsCompilerGeneratedType(rt) && IsCompilerGeneratedMethod(m))
                        continue;

                    var fullType = rt.FullName ?? rt.Name ?? string.Empty;

                    if (StartsWithAny(fullType, _skipNamespaces))
                        continue;

                    if (_skipTypes.Contains(fullType))
                        continue;

                    if (m.Name.Contains("Invoke"))
                        continue;

                    if (extraSkipPredicate != null && extraSkipPredicate(m))
                        continue;

                    var typeName = rt.Name;
                    var methodName = rm ?? NormalizeMethodName(m);
                    var ns = rt.Namespace ?? string.Empty;

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
                        DeclaringType = rt,
                        AssemblyName = rt.Assembly?.GetName()?.Name,
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
                // fall through
            }

            return new Info { Label = "<unknown>" };
        }

        /// <summary>
        /// Returns the Nth meaningful frame starting at the current method.
        /// </summary>
        public static Info GetNth(
            int n,
            int skip = 0,
            int maxFrames = 24,
            bool includeFileInfo = false,
            bool includeNamespaceInLabel = false,
            Func<MethodBase, bool> extraSkipPredicate = null
        )
        {
            if (n < 0)
                n = 0;

            try
            {
                var st = new StackTrace(skipFrames: 1 + skip, fNeedFileInfo: includeFileInfo);
                var count = Math.Min(st.FrameCount, Math.Max(4, maxFrames));

                int found = -1;

                for (int i = 0; i < count; i++)
                {
                    var frame = st.GetFrame(i);
                    var m = frame?.GetMethod();
                    if (m == null)
                        continue;

                    ResolveToUserFrame(m, out var rt, out var rm);
                    if (rt == null)
                        continue;

                    // If we still can't escape compiler-gen, keep walking.
                    if (IsCompilerGeneratedType(rt) && IsCompilerGeneratedMethod(m))
                        continue;

                    var fullType = rt.FullName ?? rt.Name ?? string.Empty;

                    if (StartsWithAny(fullType, _skipNamespaces))
                        continue;

                    if (_skipTypes.Contains(fullType))
                        continue;

                    if (m.Name.Contains("Invoke"))
                        continue;

                    if (extraSkipPredicate != null && extraSkipPredicate(m))
                        continue;

                    found++;

                    if (found == n)
                    {
                        var typeName = rt.Name;
                        var methodName = rm ?? NormalizeMethodName(m);
                        var ns = rt.Namespace ?? string.Empty;

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
                            DeclaringType = rt,
                            AssemblyName = rt.Assembly?.GetName()?.Name,
                            Namespace = ns,
                            TypeName = typeName,
                            MethodName = methodName,
                            Label = label,
                            FileName = file,
                            Line = line,
                        };
                    }
                }
            }
            catch
            {
                // fall through
            }

            return new Info { Label = "<unknown>" };
        }

        /// <summary>
        /// Convenience method to get just a label for the caller.
        /// </summary>
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

        /// <summary>
        /// Checks if the given string starts with any of the provided prefixes.
        /// </summary>
        private static bool StartsWithAny(string s, string[] prefixes)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            for (int i = 0; i < prefixes.Length; i++)
            {
                if (s.StartsWith(prefixes[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Normalizes method names, converting constructors to type names.
        /// </summary>
        private static string NormalizeMethodName(MethodBase m)
        {
            var n = m.Name;
            if (n == ".ctor" || n == ".cctor")
                return m.DeclaringType?.Name ?? n;

            return n;
        }

        /// <summary>
        /// Convenience: get the direct caller of the current method (one frame above).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Info GetCallerAbove(
            int up = 1,
            int skip = 0,
            bool includeNamespaceInLabel = false
        )
        {
            return GetNth(n: up, skip: skip, includeNamespaceInLabel: includeNamespaceInLabel);
        }

        /// <summary>
        /// Returns only the label for the frame 'up' above the current method.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCallerAboveLabel(
            int up = 1,
            int skip = 0,
            bool includeNamespace = false
        )
        {
            return GetCallerAbove(up, skip, includeNamespace)?.Label ?? "<unknown>";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Determines if the given type is compiler-generated.
        /// </summary>
        private static bool IsCompilerGeneratedType(Type t)
        {
            if (t == null)
                return false;

            // Attribute first (most reliable)
            if (t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                return true;

            // Common name patterns
            var n = t.Name ?? string.Empty;

            return n.StartsWith("<", StringComparison.Ordinal)
                || n.StartsWith("<>", StringComparison.Ordinal)
                || n.Contains("DisplayClass")
                || n.Contains("AnonStorey");
        }

        /// <summary>
        /// Determines if the given method is compiler-generated.
        /// </summary>
        private static bool IsCompilerGeneratedMethod(MethodBase m)
        {
            if (m == null)
                return false;

            if (m.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                return true;

            var n = m.Name ?? string.Empty;

            // Local funcs / lambdas / async/iter plumbing
            return n.StartsWith("<", StringComparison.Ordinal)
                || n.Contains("g__")
                || n.Contains("b__")
                || n == "MoveNext";
        }

        /// <summary>
        /// Extracts the substring between angle brackets in a string.
        /// </summary>
        private static string ExtractBetweenAngles(string s)
        {
            if (string.IsNullOrEmpty(s))
                return null;

            int a = s.IndexOf('<');
            if (a < 0)
                return null;

            int b = s.IndexOf('>', a + 1);
            if (b <= a + 1)
                return null;

            return s.Substring(a + 1, b - a - 1);
        }

        /// <summary>
        /// Converts compiler-generated frames (display classes, MoveNext state machines, local functions)
        /// into a stable "real" (Type, Method) pair.
        /// </summary>
        private static void ResolveToUserFrame(
            MethodBase m,
            out Type resolvedType,
            out string resolvedMethodName
        )
        {
            resolvedType = m?.DeclaringType;
            resolvedMethodName = NormalizeMethodName(m);

            if (m == null || resolvedType == null)
                return;

            var t = resolvedType;
            var isCg = IsCompilerGeneratedType(t) || IsCompilerGeneratedMethod(m);

            if (!isCg)
                return;

            // Prefer the outer type when this is a nested compiler-generated helper
            if (t.DeclaringType != null)
                resolvedType = t.DeclaringType;

            // Iterator/async state machine: <Outer>d__XX.MoveNext -> Outer
            if (m.Name == "MoveNext")
            {
                var outer = ExtractBetweenAngles(t.Name);
                if (!string.IsNullOrEmpty(outer))
                {
                    resolvedMethodName = outer;
                    return;
                }
            }

            // Local function / lambda: <Outer>g__Local|0 or <Outer>b__... -> Outer
            var outerFromMethod = ExtractBetweenAngles(m.Name);
            if (!string.IsNullOrEmpty(outerFromMethod))
                resolvedMethodName = outerFromMethod;
        }
    }
}
