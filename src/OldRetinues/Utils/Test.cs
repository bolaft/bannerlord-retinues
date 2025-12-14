using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace OldRetinues.Utils
{
    /// <summary>
    /// Marks a static method as an in-game test case that can be discovered and run by the Tests framework.
    /// Methods must be static, return void, and accept either no parameters or a single GameTestContext.
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

    /// <summary>
    /// Assertion failure used by the in-game test framework.
    /// </summary>
    public sealed class GameTestAssertionException : Exception
    {
        public GameTestAssertionException(string message)
            : base(message) { }

        public GameTestAssertionException(string message, Exception inner)
            : base(message, inner) { }
    }

    /// <summary>
    /// Context object passed to tests, exposing convenient accessors for campaign state and wrappers.
    /// </summary>
    public sealed class GameTestContext
    {
        /// <summary>
        /// Convenience helper to ensure we are in a running campaign.
        /// Throws if not.
        /// </summary>
        public void EnsureCampaign()
        {
            if (Campaign.Current == null)
                throw new GameTestAssertionException(
                    "No active campaign. Load a test save before running tests."
                );
        }
    }

    /// <summary>
    /// Lightweight result of a single test execution.
    /// </summary>
    public sealed class GameTestResult(
        string name,
        string group,
        string description,
        bool passed,
        string message,
        Exception exception,
        TimeSpan duration
    )
    {
        public string Name { get; } = name;
        public string Group { get; } = group;
        public string Description { get; } = description;
        public bool Passed { get; } = passed;
        public string Message { get; } = message;
        public Exception Exception { get; } = exception;
        public TimeSpan Duration { get; } = duration;
    }

    /// <summary>
    /// In-game unit test framework. Discovers [GameTest] methods in the Retinues assembly,
    /// executes them, and produces a human-readable summary for the cheat console and debug log.
    /// </summary>
    [SafeClass]
    public static class Tests
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Internals                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private sealed class RegisteredTest
        {
            public string Name { get; }
            public string Group { get; }
            public string Description { get; }
            public Action<GameTestContext> Action { get; }

            public RegisteredTest(
                string name,
                string group,
                string description,
                Action<GameTestContext> action
            )
            {
                Name = name;
                Group = group;
                Description = description;
                Action = action;
            }
        }

        private static readonly List<RegisteredTest> _tests = [];
        private static bool _discovered;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Discovery                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensure tests are discovered exactly once by scanning the Retinues assembly for [GameTest] methods.
        /// </summary>
        private static void EnsureDiscovered()
        {
            if (_discovered)
                return;

            _discovered = true;

            try
            {
                var asm = typeof(Tests).Assembly;
                var bindingFlags =
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                foreach (var type in asm.GetTypes())
                {
                    foreach (var method in type.GetMethods(bindingFlags))
                    {
                        var attr = method.GetCustomAttribute<GameTestAttribute>();
                        if (attr == null)
                            continue;

                        var parameters = method.GetParameters();

                        // Valid signatures:
                        //  - void Test()
                        //  - void Test(GameTestContext ctx)
                        if (method.ReturnType != typeof(void))
                            continue;

                        if (parameters.Length > 1)
                            continue;

                        bool acceptsContext =
                            parameters.Length == 1
                            && parameters[0].ParameterType == typeof(GameTestContext);

                        Action<GameTestContext> action = ctx =>
                        {
                            object[] args = acceptsContext ? new object[] { ctx } : null;

                            method.Invoke(null, args);
                        };

                        RegisterInternal(
                            attr.Name ?? method.Name,
                            attr.Group ?? "default",
                            attr.Description,
                            action
                        );
                    }
                }

                Log.Info(
                    $"[Tests] Discovered {_tests.Count} in-game tests in assembly {typeof(Tests).Assembly.GetName().Name}."
                );
            }
            catch (Exception ex)
            {
                Log.Error($"[Tests] Failed to discover in-game tests: {ex}");
            }
        }

        private static void RegisterInternal(
            string name,
            string group,
            string description,
            Action<GameTestContext> action
        )
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "UnnamedTest";

            var existing = _tests.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.Group, group, StringComparison.OrdinalIgnoreCase)
            );
            if (existing != null)
            {
                Log.Warn($"[Tests] Duplicate test registration ignored for {group}.{name}.");
                return;
            }

            _tests.Add(new RegisteredTest(name, group, description, action));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        API (Run)                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Runs all discovered tests. Optionally filters by group or name and stops on first failure.
        /// Returns a formatted summary suitable for the cheat console.
        /// </summary>
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

            Log.Info(
                $"[Tests] Starting in-game test run. Count={filtered.Count}, GroupFilter={groupFilter ?? "*"}, NameFilter={nameFilter ?? "*"}."
            );

            foreach (var test in filtered)
            {
                var sw = Stopwatch.StartNew();
                bool passed = false;
                string msg;
                Exception ex = null;

                try
                {
                    test.Action(ctx);
                    passed = true;
                    msg = "OK";
                }
                catch (GameTestAssertionException aex)
                {
                    msg = aex.Message;
                    ex = aex;
                }
                catch (Exception e)
                {
                    msg = "Unexpected exception: " + e.Message;
                    ex = e;
                }

                sw.Stop();

                var result = new GameTestResult(
                    test.Name,
                    test.Group,
                    test.Description,
                    passed,
                    msg,
                    ex,
                    sw.Elapsed
                );
                results.Add(result);

                if (passed)
                {
                    Log.Debug(
                        $"[Tests] PASS: {test.Group}.{test.Name} in {sw.ElapsedMilliseconds} ms. {test.Description}"
                    );
                }
                else
                {
                    Log.Error(
                        $"[Tests] FAIL: {test.Group}.{test.Name} in {sw.ElapsedMilliseconds} ms. {msg}\n{ex}"
                    );
                    if (stopOnFirstFailure)
                        break;
                }
            }

            swTotal.Stop();
            return FormatSummary(results, swTotal.Elapsed);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Assertions API                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static void AssertTrue(
            bool condition,
            string message = null,
            [CallerMemberName] string member = null
        )
        {
            if (condition)
                return;

            var msg = message ?? "Expected condition to be true.";
            throw new GameTestAssertionException($"[{member}] {msg}");
        }

        public static void AssertFalse(
            bool condition,
            string message = null,
            [CallerMemberName] string member = null
        )
        {
            if (!condition)
                return;

            var msg = message ?? "Expected condition to be false.";
            throw new GameTestAssertionException($"[{member}] {msg}");
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

            var msg = message ?? "Values are not equal.";
            throw new GameTestAssertionException(
                $"[{member}] {msg} Expected={expected}, Actual={actual}"
            );
        }

        public static void AssertNotNull(
            object value,
            string message = null,
            [CallerMemberName] string member = null
        )
        {
            if (value != null)
                return;

            var msg = message ?? "Value was null.";
            throw new GameTestAssertionException($"[{member}] {msg}");
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Summary formatting                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string FormatSummary(
            IReadOnlyCollection<GameTestResult> results,
            TimeSpan totalDuration
        )
        {
            int total = results.Count;
            int passed = results.Count(r => r.Passed);
            int failed = total - passed;

            var sb = new StringBuilder();
            sb.AppendLine(
                $"[Tests] Run complete. Total={total}, Passed={passed}, Failed={failed}, Duration={totalDuration.TotalMilliseconds:F0} ms."
            );

            foreach (var r in results.OrderBy(r => r.Group).ThenBy(r => r.Name))
            {
                var status = r.Passed ? "PASS" : "FAIL";
                sb.AppendLine(
                    $" - [{status}] {r.Group}.{r.Name} ({r.Duration.TotalMilliseconds:F0} ms) : {r.Message}"
                );
            }

            return sb.ToString();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("run_tests", "retinues")]
        public static string RunTests(List<string> args)
        {
            // Example: retinues.run_tests [group] [name-substring] [--stop]
            string group = null;
            string name = null;
            bool stopOnFirstFailure = false;

            if (args.Count > 0)
                group = args[0] == "-" ? null : args[0];
            if (args.Count > 1)
                name = args[1] == "-" ? null : args[1];
            if (args.Count > 2 && args[2] == "--stop")
                stopOnFirstFailure = true;

            return RunAllTests(group, name, stopOnFirstFailure);
        }
    }
}
