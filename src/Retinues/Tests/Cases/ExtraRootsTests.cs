using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Factions.Wrappers;
using TaleWorlds.CampaignSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for the discovery of extra troop trees (secondary soldier trees outside every
    /// canonical culture roster, e.g. Realm of Thrones' Household Troops).
    /// </summary>
    public static class ExtraRootsTests
    {
        /// <summary>
        /// Builds the set of ids that some soldier troop upgrades into.
        /// </summary>
        private static HashSet<string> BuildUpgradedInto()
        {
            var set = new HashSet<string>(System.StringComparer.Ordinal);

            foreach (var wc in WCharacter.All)
            {
                if (wc?.Base == null || wc.IsHero)
                    continue;

                var targets = wc.Base.UpgradeTargets;
                if (targets == null)
                    continue;

                foreach (var t in targets)
                    if (!string.IsNullOrEmpty(t?.StringId))
                        set.Add(t.StringId);
            }

            return set;
        }

        [GameTest(
            "ExtraRootsSatisfyDiscoveryContract",
            "editor",
            "Every discovered extra root is an unflagged, visible, non-custom soldier tree root of its culture"
        )]
        public static void ExtraRootsSatisfyDiscoveryContract(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            ExtraRootsCache.Invalidate();
            var upgradedInto = BuildUpgradedInto();

            foreach (var culture in WCulture.All)
            {
                if (culture?.Base == null)
                    continue;

                var canonical = new HashSet<string>(System.StringComparer.Ordinal);
                void AddId(WCharacter c)
                {
                    if (!string.IsNullOrEmpty(c?.StringId))
                        canonical.Add(c.StringId);
                }
                AddId(culture.RootBasic);
                AddId(culture.RootElite);
                AddId(culture.MeleeMilitiaTroop);
                AddId(culture.RangedMilitiaTroop);
                AddId(culture.CaravanGuard);
                AddId(culture.Villager);

                foreach (var root in culture.ExtraRoots)
                {
                    Tests.AssertNotNull(root?.Base, "Extra root wraps a live troop.");
                    Tests.AssertFalse(root.IsHero, $"'{root.StringId}' is not a hero.");
                    Tests.AssertFalse(root.IsCustom, $"'{root.StringId}' is not a custom stub.");
                    Tests.AssertFalse(
                        root.HiddenInEncyclopedia,
                        $"'{root.StringId}' is encyclopedia-visible."
                    );
                    Tests.AssertEqual(
                        culture.StringId,
                        root.Culture?.StringId,
                        $"'{root.StringId}' belongs to the culture it is listed under."
                    );
                    Tests.AssertTrue(
                        SourceFlagCache.Get(root) == TroopSourceFlags.None,
                        $"'{root.StringId}' is claimed by no known roster."
                    );
                    Tests.AssertFalse(
                        upgradedInto.Contains(root.StringId),
                        $"'{root.StringId}' is a true root: nothing upgrades into it."
                    );
                    Tests.AssertFalse(
                        canonical.Contains(root.StringId),
                        $"'{root.StringId}' is not one of the culture's canonical troops."
                    );
                }
            }
        }

        [GameTest(
            "ExtraRootsReactToVisibility",
            "editor",
            "A hidden qualifying soldier tree appears in ExtraRoots once made visible, and vanishes again"
        )]
        public static void ExtraRootsReactToVisibility(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var upgradedInto = BuildUpgradedInto();

            // Find a troop that qualifies on every axis EXCEPT visibility. Vanilla ships hidden
            // quest/conspiracy soldiers that fit; if this load order has none, there is nothing
            // to exercise.
            var candidate = WCharacter.All.FirstOrDefault(wc =>
                wc?.Base != null
                && !wc.IsHero
                && !wc.IsCustom
                && wc.Base.Occupation == Occupation.Soldier
                && wc.HiddenInEncyclopedia
                && wc.Culture?.StringId != null
                && SourceFlagCache.Get(wc) == TroopSourceFlags.None
                && !upgradedInto.Contains(wc.StringId)
            );

            if (candidate == null)
                return; // No hidden qualifying troop in this load order; nothing to assert.

            var cultureId = candidate.Culture.StringId;

            bool Listed() =>
                ExtraRootsCache.Get(cultureId).Any(r => r?.StringId == candidate.StringId);

            try
            {
                ExtraRootsCache.Invalidate();
                Tests.AssertFalse(Listed(), "Hidden troop is not listed as an extra root.");

                candidate.HiddenInEncyclopedia = false;
                ExtraRootsCache.Invalidate();
                Tests.AssertTrue(
                    Listed(),
                    "The troop appears as an extra root once made visible."
                );
            }
            finally
            {
                candidate.HiddenInEncyclopedia = true;
                candidate.MarkAllAttributesClean(); // do not persist the test's visibility flip
                ExtraRootsCache.Invalidate();
            }

            Tests.AssertFalse(Listed(), "The troop disappears again after being re-hidden.");
        }
    }
}
