using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Retinues.Tests.Cases
{
    public static class RunnerTests
    {
        [GameTest("AssertionsCannotBeSwallowed", "framework", RequiresCampaign = false)]
        public static void AssertionsCannotBeSwallowed()
        {
            Tests.AssertFalse(typeof(Tests).GetCustomAttributes(false).Any(a => a.GetType().Name == "SafeClassAttribute"),
                "Assertion helpers must never be wrapped by exception-swallowing safety patches.");
            Tests.AssertThrows<GameTestAssertionException>(() => Tests.AssertTrue(false, "Intentional runner self-test."));
            Tests.AssertThrows<GameTestAssertionException>(() => Tests.AssertEqual(1, 2));
        }

        [GameTest("FailuresSkipsAndEmptyTestsAreDistinct", "framework", RequiresCampaign = false)]
        public static void FailuresSkipsAndEmptyTestsAreDistinct()
        {
            GameTestResult Run(Action<GameTestContext> action) =>
                Tests.ExecuteCase(new GameTestCase("fixture", "self-test", null, false, action), 17);
            Tests.AssertEqual(TestOutcome.Failed, Run(_ => Tests.AssertTrue(false)).Outcome);
            Tests.AssertEqual(TestOutcome.Skipped, Run(_ => Tests.Skip("No DLC fixture.")).Outcome);
            Tests.AssertEqual(TestOutcome.Failed, Run(_ => { }).Outcome, "An early return is not proof of success.");
            var passed = Run(_ => Tests.AssertEqual(4, 2 + 2));
            Tests.AssertEqual(TestOutcome.Passed, passed.Outcome);
            Tests.AssertEqual(1, passed.Assertions, "Nested test assertion counters are isolated.");
        }

        [GameTest("CleanupRunsInReverseAndFailureCannotPass", "framework", RequiresCampaign = false)]
        public static void CleanupRunsInReverseAndFailureCannotPass()
        {
            var order = new List<int>();
            var result = Tests.ExecuteCase(new GameTestCase("cleanup", "self-test", null, false, context =>
            {
                context.Defer(() => order.Add(1));
                context.Defer(() => { order.Add(2); throw new InvalidOperationException("cleanup fault"); });
                context.Defer(() => order.Add(3));
                Tests.AssertTrue(true);
            }), 19);
            Tests.AssertTrue(order.SequenceEqual(new[] { 3, 2, 1 }), "All cleanup actions run, even after one fails.");
            Tests.AssertEqual(TestOutcome.Failed, result.Outcome);
            Tests.AssertTrue(result.StateLeaked, "Cleanup failure requires stopping the suite.");
            var dispose = Tests.ExecuteCase(new GameTestCase("dispose", "self-test", null, false, _ =>
                throw new GameTestCleanupException("scope failed", new Exception("fixture"))), 19);
            Tests.AssertTrue(dispose.StateLeaked, "IDisposable cleanup failure also stops the suite.");
        }

        [GameTest("SeedReproducesGeneratedInputs", "framework", RequiresCampaign = false)]
        public static void SeedReproducesGeneratedInputs()
        {
            var values = new List<int>();
            var test = new GameTestCase("seed", "self-test", null, false, context =>
            {
                values.Add(context.Random.Next());
                Tests.AssertTrue(true);
            });
            Tests.ExecuteCase(test, 123);
            Tests.ExecuteCase(test, 123);
            Tests.AssertEqual(values[0], values[1], "The same reported case seed recreates the same random inputs.");
        }

        [GameTest("JUnitPreservesFailuresAndSkips", "framework", RequiresCampaign = false)]
        public static void JUnitPreservesFailuresAndSkips()
        {
            var run = new GameTestRun(42, 1) { Planned = 2 };
            run.Results.Add(new GameTestResult("failure", "fixture", TestOutcome.Failed, "bad <value> & data",
                TimeSpan.Zero, 42, 0, 1));
            run.Results.Add(new GameTestResult("skip", "fixture", TestOutcome.Skipped, "No naval DLC",
                TimeSpan.Zero, 43, 0, 0));
            var xml = XDocument.Parse(run.ToJUnit());
            var suite = xml.Root.Element("testsuite");
            Tests.AssertEqual("1", (string)suite.Attribute("failures"));
            Tests.AssertEqual("1", (string)suite.Attribute("skipped"));
            Tests.AssertEqual("bad <value> & data", (string)suite.Element("testcase").Element("failure").Attribute("message"));
            Tests.AssertFalse(run.Success, "Neither missing coverage nor failed checks produce a green run.");
        }
    }
}
