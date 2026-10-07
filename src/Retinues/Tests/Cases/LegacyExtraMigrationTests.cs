using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Migration;
using Retinues.Migration.Legacy;

namespace Retinues.Tests.Cases
{
    public static class LegacyExtraMigrationTests
    {
        [GameTest("LegacyExtraTreesReachTheMigrationPass", "migration", RequiresCampaign = false)]
        public static void LegacyExtraTreesReachTheMigrationPass()
        {
            var child = new TroopSaveData { StringId = "extra_child", Name = "Edited child", Level = 29 };
            var captain = new TroopSaveData { StringId = "extra_captain", IsCaptain = true };
            var extra = new TroopSaveData { StringId = "extra_root", UpgradeTargets = new List<TroopSaveData> { child }, Captain = captain };
            var faction = new FactionSaveData { Extras = new List<TroopSaveData> { extra } };
            var flatten = typeof(LegacyMigrationCoordinator).GetMethod("FlattenFaction", BindingFlags.Static | BindingFlags.NonPublic);
            Tests.AssertNotNull(flatten);
            var migrated = ((IEnumerable<TroopSaveData>)flatten.Invoke(null, new object[] { faction })).ToList();
            Tests.AssertTrue(migrated.Contains(extra), "An extra root reaches character migration even without a canonical root.");
            Tests.AssertTrue(migrated.Contains(child), "An edited descendant is preserved.");
            Tests.AssertTrue(migrated.Contains(captain), "Extra-tree captains are preserved.");
            Tests.AssertEqual(3, migrated.Count);
            faction.Extras = null;
            Tests.AssertFalse(((IEnumerable<TroopSaveData>)flatten.Invoke(null, new object[] { faction })).Any(),
                "Older saves without field 17 still load.");
        }
    }
}
