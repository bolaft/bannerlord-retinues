using System.Linq;
using Retinues.Behaviors.Retinues;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Settings;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for retinue creation invariants.
    ///
    /// A retinue inherits the level of its culture's elite root, which troop overhauls can redefine
    /// to a very low-level unit (Warlords Battlefield's Vlandian noble is level 3 -> tier 0). A tier
    /// 0 retinue can never be recruited, because conversion looks for a source exactly one tier
    /// below and nothing is tier -1.
    /// </summary>
    public static class RetinueCreationTests
    {
        [GameTest(
            "RetinueIsNeverTierZero",
            "retinues",
            "A retinue created from a very low-level culture root is still at least tier 1"
        )]
        public static void RetinueIsNeverTierZero(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            if (!Configuration.EnableRetinues)
                return; // Feature disabled; creation always returns null.

            var behavior = RetinuesBehavior.Instance;
            Tests.AssertNotNull(behavior, "The retinues behavior is registered.");

            var culture = WCulture.All.FirstOrDefault(c =>
                c?.Base != null && (c.RootElite ?? c.RootBasic)?.Base != null
            );
            Tests.AssertNotNull(culture, "Found a culture with a troop root to clone from.");

            using var sandbox = new TestSandbox();

            var retinue = sandbox.Track(
                behavior.CreateRetinue(culture, "Test Retinue", notifyUnlocks: false)
            );
            Tests.AssertNotNull(retinue, "A retinue was created from the culture root.");

            // The core invariant: tier 0 makes conversion impossible (it would search for tier -1).
            Tests.AssertTrue(
                retinue.Tier >= 1,
                $"A new retinue is at least tier 1 (got tier {retinue.Tier}, level {retinue.Level})."
            );
        }

        [GameTest(
            "NormalTemplateLevelIsInherited",
            "retinues",
            "A culture root already at tier 1+ still passes its own level through to the retinue"
        )]
        public static void NormalTemplateLevelIsInherited(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            if (!Configuration.EnableRetinues)
                return;

            var behavior = RetinuesBehavior.Instance;
            Tests.AssertNotNull(behavior, "The retinues behavior is registered.");

            // Pick a culture whose root is already a normal tier, so the override must NOT kick in.
            var culture = WCulture.All.FirstOrDefault(c =>
            {
                var root = c?.Base == null ? null : (c.RootElite ?? c.RootBasic);
                return root?.Base != null && root.Tier >= 1;
            });
            Tests.AssertNotNull(culture, "Found a culture whose root is tier 1 or above.");

            var template = culture.RootElite ?? culture.RootBasic;

            using var sandbox = new TestSandbox();
            var retinue = sandbox.Track(
                behavior.CreateRetinue(culture, "Test Retinue", notifyUnlocks: false)
            );
            Tests.AssertNotNull(retinue, "A retinue was created.");

            // The floor only corrects tier 0; a normal template's level is inherited untouched.
            Tests.AssertEqual(
                template.Level,
                retinue.Level,
                "A tier 1+ template's level is inherited rather than overridden."
            );
        }
    }
}
