using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;

namespace Retinues.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class GameTestAttribute(string name, string group = "default", string description = null) : Attribute
    {
        public string Name { get; } = name;
        public string Group { get; } = group;
        public string Description { get; } = description;
        // Explicit opt-in: a method with no arguments can still require the game.
        public bool RequiresCampaign { get; set; } = true;
    }

    public sealed class GameTestAssertionException : Exception
    {
        public GameTestAssertionException(string message) : base(message) { }
        public GameTestAssertionException(string message, Exception inner) : base(message, inner) { }
    }

    public sealed class GameTestSkipException(string reason) : Exception(reason) { }

    public sealed class GameTestCleanupException(string reason, Exception inner) : Exception(reason, inner) { }

    public sealed class GameTestContext(int seed = 12345)
    {
        private readonly Stack<Action> _cleanup = new();
        public int Seed { get; } = seed;
        public Random Random { get; } = new Random(seed);
        public int Assertions { get; private set; }
        internal void CountAssertion() => Assertions++;

        public void EnsureCampaign()
        {
            if (Campaign.Current == null)
                Tests.Skip("Requires a loaded test campaign.");
        }

        public void Defer(Action cleanup)
        {
            if (cleanup == null)
                throw new ArgumentNullException(nameof(cleanup));
            _cleanup.Push(cleanup);
        }

        internal List<Exception> Cleanup()
        {
            var errors = new List<Exception>();
            while (_cleanup.Count > 0)
                try { _cleanup.Pop()(); }
                catch (Exception e) { errors.Add(e); }
            return errors;
        }

        public void Case(int index, Action action)
        {
            try { action(); }
            catch (GameTestSkipException) { throw; }
            catch (GameTestCleanupException) { throw; }
            catch (Exception e)
            {
                throw new GameTestAssertionException($"Seed={Seed}, generated case={index}: {e.Message}", e);
            }
        }
    }

    public enum TestOutcome { Passed, Failed, Skipped }

    public sealed class GameTestCase(
        string name, string group, string description, bool requiresCampaign, Action<GameTestContext> action)
    {
        public string Name { get; } = name;
        public string Group { get; } = group;
        public string Description { get; } = description;
        public bool RequiresCampaign { get; } = requiresCampaign;
        internal Action<GameTestContext> Action { get; } = action;
    }

    public sealed class GameTestResult(
        string name, string group, TestOutcome outcome, string message, TimeSpan duration,
        int seed, int iteration, int assertions)
    {
        public string Name { get; } = name;
        public string Group { get; } = group;
        public TestOutcome Outcome { get; } = outcome;
        public bool Passed => Outcome == TestOutcome.Passed;
        public string Message { get; } = message;
        public TimeSpan Duration { get; } = duration;
        public int Seed { get; } = seed;
        public int Iteration { get; } = iteration;
        public int Assertions { get; } = assertions;
        public bool StateLeaked { get; internal set; }
    }

    public sealed class GameTestRun(int seed, int repeat)
    {
        public int Seed { get; } = seed;
        public int Repeat { get; } = repeat;
        public int Planned { get; internal set; }
        public List<GameTestResult> Results { get; } = new();
        public int Passed => Results.Count(r => r.Outcome == TestOutcome.Passed);
        public int Failed => Results.Count(r => r.Outcome == TestOutcome.Failed);
        public int Skipped => Results.Count(r => r.Outcome == TestOutcome.Skipped);
        public bool Success => Planned > 0 && Results.Count == Planned && Passed == Planned;

        public string ToJUnit()
        {
            string target =
#if BL12
                "BL12";
#elif BL13
                "BL13";
#else
                "BL14";
#endif
            var suite = new XElement("testsuite",
                new XAttribute("name", "Retinues"),
                new XAttribute("tests", Results.Count),
                new XAttribute("failures", Failed),
                new XAttribute("skipped", Skipped),
                new XAttribute("errors", 0),
                new XAttribute("time", Results.Sum(r => r.Duration.TotalSeconds).ToString("R", CultureInfo.InvariantCulture)),
                new XElement("properties",
                    new XElement("property", new XAttribute("name", "seed"), new XAttribute("value", Seed)),
                    new XElement("property", new XAttribute("name", "repeat"), new XAttribute("value", Repeat)),
                    new XElement("property", new XAttribute("name", "planned"), new XAttribute("value", Planned)),
                    new XElement("property", new XAttribute("name", "buildTarget"), new XAttribute("value", target)),
                    new XElement("property", new XAttribute("name", "gameAssembly"),
                        new XAttribute("value", FileVersionInfo.GetVersionInfo(typeof(Campaign).Assembly.Location).FileVersion ?? "unknown"))));
            foreach (var result in Results)
            {
                var entry = new XElement("testcase",
                    new XAttribute("classname", result.Group),
                    new XAttribute("name", result.Name + " [repeat " + (result.Iteration + 1) + "]"),
                    new XAttribute("time", result.Duration.TotalSeconds.ToString("R", CultureInfo.InvariantCulture)));
                if (result.Outcome == TestOutcome.Failed)
                    entry.Add(new XElement("failure", new XAttribute("message", result.Message), result.Message));
                else if (result.Outcome == TestOutcome.Skipped)
                    entry.Add(new XElement("skipped", new XAttribute("message", result.Message)));
                entry.Add(new XElement("system-out", $"seed={result.Seed}; assertions={result.Assertions}; stateLeaked={result.StateLeaked}"));
                suite.Add(entry);
            }
            return new XDocument(new XElement("testsuites", suite)).ToString();
        }

        public void WriteJUnit(string path)
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, ToJUnit());
        }
    }
}
