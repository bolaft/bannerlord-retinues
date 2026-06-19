using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Tests
{
    /// <summary>
    /// Marks a static method as an in-game test case discovered and run by the Tests framework.
    /// Methods must be static, return void, and accept either no parameters or a single
    /// GameTestContext.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class GameTestAttribute(
        string name,
        string group = "default",
        string description = null
    ) : Attribute
    {
        public string Name { get; } = name;
        public string Group { get; } = group;
        public string Description { get; } = description;
    }

    /// <summary>Assertion failure used by the in-game test framework.</summary>
    public sealed class GameTestAssertionException : Exception
    {
        public GameTestAssertionException(string message)
            : base(message) { }
    }

    /// <summary>Context passed to tests, exposing campaign-state helpers.</summary>
    public sealed class GameTestContext
    {
        /// <summary>Ensures a campaign is running; throws otherwise.</summary>
        public void EnsureCampaign()
        {
            if (Campaign.Current == null)
                throw new GameTestAssertionException(
                    "No active campaign. Load a test save before running tests."
                );
        }
    }

    /// <summary>Result of a single test execution.</summary>
    public sealed class GameTestResult(
        string name,
        string group,
        bool passed,
        string message,
        TimeSpan duration
    )
    {
        public string Name { get; } = name;
        public string Group { get; } = group;
        public bool Passed { get; } = passed;
        public string Message { get; } = message;
        public TimeSpan Duration { get; } = duration;
    }

    /// <summary>
    /// In-game test framework. Discovers [GameTest] methods in the Retinues assembly, runs them,
    /// and produces a human-readable summary for the cheat console and debug log.
    ///
    /// Deliberately NOT [SafeClass]: the safe-method finalizer would swallow assertion exceptions
    /// and silently pass failing tests. Per-test isolation lives in RunAllTests' try/catch.
    /// </summary>
    public static class Tests
    {
        private sealed class RegisteredTest(
            string name,
            string group,
            Action<GameTestContext> action
        )
        {
            public string Name { get; } = name;
            public string Group { get; } = group;
            public Action<GameTestContext> Action { get; } = action;
        }

        private static readonly List<RegisteredTest> _tests = [];
        private static bool _discovered;

        // ─── Discovery ──────────────────────────────────────────────────────

        private static void EnsureDiscovered()
        {
            if (_discovered)
                return;
            _discovered = true;

            try
            {
                var asm = typeof(Tests).Assembly;
                var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                foreach (var type in asm.GetTypes())
                foreach (var method in type.GetMethods(flags))
                {
                    var attr = method.GetCustomAttribute<GameTestAttribute>();
                    if (attr == null || method.ReturnType != typeof(void))
                        continue;

                    var parameters = method.GetParameters();
                    if (parameters.Length > 1)
                        continue;

                    bool acceptsContext =
                        parameters.Length == 1
                        && parameters[0].ParameterType == typeof(GameTestContext);

                    void Action(GameTestContext ctx) =>
                        method.Invoke(null, acceptsContext ? [ctx] : null);

                    Register(attr.Name ?? method.Name, attr.Group ?? "default", Action);
                }

                Log.Info($"[Tests] Discovered {_tests.Count} in-game tests.");
            }
            catch (Exception ex)
            {
                Log.Error($"[Tests] Failed to discover in-game tests: {ex}");
            }
        }

        private static void Register(string name, string group, Action<GameTestContext> action)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "UnnamedTest";

            if (
                _tests.Any(t =>
                    string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.Group, group, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                Log.Warning($"[Tests] Duplicate test ignored: {group}.{name}.");
                return;
            }

            _tests.Add(new RegisteredTest(name, group, action));
        }

        // ─── Run ────────────────────────────────────────────────────────────

        public static string RunAllTests(
            string groupFilter = null,
            string nameFilter = null,
            bool stopOnFirstFailure = false
        )
        {
            EnsureDiscovered();

            var ctx = new GameTestContext();
            ctx.EnsureCampaign();

            var filtered = _tests
                .Where(t =>
                    (
                        string.IsNullOrEmpty(groupFilter)
                        || t.Group.Equals(groupFilter, StringComparison.OrdinalIgnoreCase)
                    )
                    && (
                        string.IsNullOrEmpty(nameFilter)
                        || t.Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0
                    )
                )
                .ToList();

            if (filtered.Count == 0)
                return "[Tests] No tests matched the specified filters.";

            var results = new List<GameTestResult>(filtered.Count);
            var swTotal = Stopwatch.StartNew();

            foreach (var test in filtered)
            {
                var sw = Stopwatch.StartNew();
                bool passed = false;
                string msg;

                try
                {
                    test.Action(ctx);
                    passed = true;
                    msg = "OK";
                }
                catch (GameTestAssertionException aex)
                {
                    msg = aex.Message;
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    var inner = tie.InnerException;
                    msg =
                        inner is GameTestAssertionException
                            ? inner.Message
                            : "Unexpected exception: " + inner;
                }
                catch (Exception e)
                {
                    msg = "Unexpected exception: " + e;
                }

                sw.Stop();
                results.Add(new GameTestResult(test.Name, test.Group, passed, msg, sw.Elapsed));

                if (passed)
                    Log.Debug($"[Tests] PASS: {test.Group}.{test.Name} ({sw.ElapsedMilliseconds} ms).");
                else
                {
                    Log.Error($"[Tests] FAIL: {test.Group}.{test.Name} ({sw.ElapsedMilliseconds} ms). {msg}");
                    if (stopOnFirstFailure)
                        break;
                }
            }

            swTotal.Stop();
            return FormatSummary(results, swTotal.Elapsed);
        }

        // ─── Assertions ─────────────────────────────────────────────────────

        public static void AssertTrue(
            bool condition,
            string message = null,
            [CallerMemberName] string member = null
        )
        {
            if (!condition)
                throw new GameTestAssertionException($"[{member}] {message ?? "Expected true."}");
        }

        public static void AssertFalse(
            bool condition,
            string message = null,
            [CallerMemberName] string member = null
        )
        {
            if (condition)
                throw new GameTestAssertionException($"[{member}] {message ?? "Expected false."}");
        }

        public static void AssertEqual<T>(
            T expected,
            T actual,
            string message = null,
            [CallerMemberName] string member = null
        )
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
                return;
            throw new GameTestAssertionException(
                $"[{member}] {message ?? "Values not equal."} Expected={expected}, Actual={actual}"
            );
        }

        public static void AssertNotNull(
            object value,
            string message = null,
            [CallerMemberName] string member = null
        )
        {
            if (value == null)
                throw new GameTestAssertionException($"[{member}] {message ?? "Value was null."}");
        }

        // ─── Summary ────────────────────────────────────────────────────────

        private static string FormatSummary(
            IReadOnlyCollection<GameTestResult> results,
            TimeSpan totalDuration
        )
        {
            int total = results.Count;
            int passed = results.Count(r => r.Passed);

            var sb = new StringBuilder();
            sb.AppendLine(
                $"[Tests] Run complete. Total={total}, Passed={passed}, Failed={total - passed}, "
                    + $"Duration={totalDuration.TotalMilliseconds:F0} ms."
            );

            foreach (var r in results.OrderBy(r => r.Group).ThenBy(r => r.Name))
                sb.AppendLine(
                    $" - [{(r.Passed ? "PASS" : "FAIL")}] {r.Group}.{r.Name} "
                        + $"({r.Duration.TotalMilliseconds:F0} ms) : {r.Message}"
                );

            return sb.ToString();
        }

        // ─── Console command ────────────────────────────────────────────────

        // Usage: retinues.run_tests [group] [name-substring] [--stop]
        [CommandLineFunctionality.CommandLineArgumentFunction("run_tests", "retinues")]
        public static string RunTests(List<string> args)
        {
            string group = args.Count > 0 && args[0] != "-" ? args[0] : null;
            string name = args.Count > 1 && args[1] != "-" ? args[1] : null;
            bool stop = args.Count > 2 && args[2] == "--stop";
            return RunAllTests(group, name, stop);
        }
    }
}
