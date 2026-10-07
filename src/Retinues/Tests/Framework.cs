using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Retinues.Tests
{
    // Never add SafeClass/SafeMethod here: a swallowed assertion becomes a false pass.
    public static class Tests
    {
        [ThreadStatic]
        private static GameTestContext _current;

        public static GameTestRun LastRun { get; private set; }

        public static List<GameTestCase> Discover()
        {
            var cases = new List<GameTestCase>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var type in typeof(Tests).Assembly.GetTypes())
            foreach (var method in type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var attribute = method.GetCustomAttribute<GameTestAttribute>();
                if (attribute == null)
                    continue;
                var parameters = method.GetParameters();
                if (!method.IsStatic || method.ReturnType != typeof(void) || method.ContainsGenericParameters
                    || parameters.Length > 1
                    || (parameters.Length == 1 && parameters[0].ParameterType != typeof(GameTestContext)))
                    throw new InvalidOperationException("Invalid test signature: " + method);
                if (string.IsNullOrWhiteSpace(attribute.Name) || string.IsNullOrWhiteSpace(attribute.Group))
                    throw new InvalidOperationException("A test needs a nonempty name and group: " + method);
                var id = attribute.Group + "." + attribute.Name;
                if (!ids.Add(id))
                    throw new InvalidOperationException("Duplicate test ID: " + id);
                cases.Add(new GameTestCase(attribute.Name, attribute.Group, attribute.Description,
                    attribute.RequiresCampaign, context => method.Invoke(null,
                        parameters.Length == 0 ? null : new object[] { context })));
            }
            return cases.OrderBy(t => t.Group, StringComparer.Ordinal)
                .ThenBy(t => t.Name, StringComparer.Ordinal).ToList();
        }

        public static GameTestRun RunSuite(
            string groupFilter = null, string nameFilter = null, bool headlessOnly = false,
            int seed = 12345, int repeat = 1, bool stopOnFirstFailure = false)
        {
            if (repeat < 1 || repeat > 100)
                throw new ArgumentOutOfRangeException(nameof(repeat), "Repeat must be between 1 and 100.");

            var run = new GameTestRun(seed, repeat);
            LastRun = run;
            List<GameTestCase> selected;
            try
            {
                selected = Discover().Where(t =>
                    (!headlessOnly || !t.RequiresCampaign)
                    && (string.IsNullOrEmpty(groupFilter) || t.Group.Equals(groupFilter, StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrEmpty(nameFilter) || t.Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
                if (selected.Count == 0)
                    throw new InvalidOperationException("No tests matched the requested filters.");
            }
            catch (Exception e)
            {
                run.Results.Add(new GameTestResult("Discovery", "framework", TestOutcome.Failed, e.ToString(), TimeSpan.Zero, seed, 0, 0));
                run.Planned = 1;
                return run;
            }

            run.Planned = selected.Count * repeat;
            bool stopped = false;
            for (int iteration = 0; iteration < repeat; iteration++)
            {
                var ordered = selected.ToList();
                var orderRandom = new Random(unchecked(seed + iteration));
                // Repeat runs deliberately change order to reveal shared-state dependencies.
                if (repeat > 1)
                    for (int i = ordered.Count - 1; i > 0; i--)
                    {
                        int j = orderRandom.Next(i + 1);
                        var temp = ordered[i];
                        ordered[i] = ordered[j];
                        ordered[j] = temp;
                    }
                foreach (var test in ordered)
                {
                    int caseSeed = CaseSeed(seed, test.Group + "." + test.Name, iteration);
                    var result = stopped
                        ? new GameTestResult(test.Name, test.Group, TestOutcome.Skipped,
                            "Not run after an earlier failure or state leak.", TimeSpan.Zero, caseSeed, iteration, 0)
                        : ExecuteCase(test, caseSeed, iteration);
                    run.Results.Add(result);
                    if (result.StateLeaked || (stopOnFirstFailure && result.Outcome == TestOutcome.Failed))
                        stopped = true;
                }
            }
            return run;
        }

        public static string RunAllTests(string groupFilter = null, string nameFilter = null, bool stopOnFirstFailure = false)
            => FormatSummary(RunSuite(groupFilter, nameFilter, stopOnFirstFailure: stopOnFirstFailure));

        // Exposed to verify the runner itself without registering deliberate failing tests.
        public static GameTestResult ExecuteCase(GameTestCase test, int seed, int iteration = 0)
        {
            var previous = _current;
            var context = new GameTestContext(seed);
            _current = context;
            var watch = Stopwatch.StartNew();
            var outcome = TestOutcome.Passed;
            string message = "OK";
            string before = null;
            bool leaked = false;
            try
            {
                if (test.RequiresCampaign)
                {
                    context.EnsureCampaign();
                    before = CampaignSafetySnapshot.Capture();
                }
                test.Action(context);
                if (context.Assertions == 0)
                    throw new GameTestAssertionException("Test returned without an assertion. Use Tests.Skip with a reason for missing fixtures.");
            }
            catch (Exception e)
            {
                e = Unwrap(e);
                leaked = e is GameTestCleanupException;
                outcome = e is GameTestSkipException ? TestOutcome.Skipped : TestOutcome.Failed;
                message = e is GameTestAssertionException || e is GameTestSkipException ? e.Message : e.ToString();
            }
            finally
            {
                foreach (var cleanupError in context.Cleanup())
                {
                    outcome = TestOutcome.Failed;
                    leaked = true;
                    message += "\nCleanup failed: " + cleanupError;
                }
                if (before != null)
                {
                    try
                    {
                        if (before != CampaignSafetySnapshot.Capture())
                        {
                            leaked = true;
                            outcome = TestOutcome.Failed;
                            message += "\nCampaign state changed: party rosters, player resources, or active custom troops were not restored.";
                        }
                    }
                    catch (Exception e)
                    {
                        leaked = true;
                        outcome = TestOutcome.Failed;
                        message += "\nCould not verify campaign state restoration: " + e;
                    }
                }
                _current = previous;
            }
            watch.Stop();
            return new GameTestResult(test.Name, test.Group, outcome, message, watch.Elapsed,
                seed, iteration, context.Assertions) { StateLeaked = leaked };
        }

        internal static Exception Unwrap(Exception e)
        {
            while (e is TargetInvocationException invocation && invocation.InnerException != null)
                e = invocation.InnerException;
            return e;
        }

        private static int CaseSeed(int seed, string id, int iteration)
        {
            unchecked
            {
                int hash = seed;
                foreach (char c in id)
                    hash = hash * 31 + c;
                return hash * 31 + iteration;
            }
        }

        public static void Skip(string reason) => throw new GameTestSkipException(reason);

        public static void AssertTrue(bool condition, string message = null, [CallerMemberName] string member = null)
        {
            _current?.CountAssertion();
            if (!condition)
                throw new GameTestAssertionException($"[{member}] {message ?? "Expected true."}");
        }

        public static void AssertFalse(bool condition, string message = null, [CallerMemberName] string member = null)
            => AssertTrue(!condition, message ?? "Expected false.", member);

        public static void AssertEqual<T>(T expected, T actual, string message = null, [CallerMemberName] string member = null)
            => AssertTrue(EqualityComparer<T>.Default.Equals(expected, actual),
                $"{message ?? "Values not equal."} Expected={expected}, Actual={actual}", member);

        public static void AssertNotNull(object value, string message = null, [CallerMemberName] string member = null)
            => AssertTrue(value != null, message ?? "Value was null.", member);

        public static void AssertThrows<T>(Action action, string message = null) where T : Exception
        {
            _current?.CountAssertion();
            try { action(); }
            catch (T) { return; }
            throw new GameTestAssertionException(message ?? $"Expected {typeof(T).Name}.");
        }

        public static string FormatSummary(GameTestRun run)
        {
            var text = new StringBuilder();
            text.AppendLine($"[Tests] Planned={run.Planned}, Passed={run.Passed}, Failed={run.Failed}, Skipped={run.Skipped}, Seed={run.Seed}, Repeat={run.Repeat}.");
            foreach (var result in run.Results)
                text.AppendLine($" - [{result.Outcome}] {result.Group}.{result.Name} repeat={result.Iteration + 1} seed={result.Seed} assertions={result.Assertions}: {result.Message}");
            return text.ToString();
        }

        // retinues.run_tests [group] [name] [--headless] [--seed=12345] [--repeat=3] [--stop] [--junit=path]
        [CommandLineFunctionality.CommandLineArgumentFunction("run_tests", "retinues")]
        public static string RunTests(List<string> args)
        {
            try
            {
                var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
                int seed = 12345, repeat = 1;
                bool headless = false, stop = false;
                string report = null;
                foreach (var arg in args.Where(a => a.StartsWith("--", StringComparison.Ordinal)))
                {
                    if (arg == "--headless") headless = true;
                    else if (arg == "--stop") stop = true;
                    else if (arg.StartsWith("--seed=", StringComparison.Ordinal)) seed = int.Parse(arg.Substring(7));
                    else if (arg.StartsWith("--repeat=", StringComparison.Ordinal)) repeat = int.Parse(arg.Substring(9));
                    else if (arg.StartsWith("--junit=", StringComparison.Ordinal)) report = arg.Substring(8);
                    else throw new ArgumentException("Unknown test option: " + arg);
                }
                if (positional.Count > 2)
                    throw new ArgumentException("Expected at most a group and name filter.");
                string Filter(int index) => positional.Count > index && positional[index] != "-" ? positional[index] : null;
                var run = RunSuite(Filter(0), Filter(1), headless, seed, repeat, stop);
                if (report != null)
                    run.WriteJUnit(report);
                string summary = FormatSummary(run);
                Log.Info(summary);
                return summary;
            }
            catch (Exception e) { return "[Tests] Runner failed: " + e; }
        }
    }
}
