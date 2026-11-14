using System.Runtime.Serialization;
using Retinues.Behaviors.Doctrines;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Behaviors.Retinues.Patches;
using Retinues.Domain.Characters.Services.Caches;
using Retinues.Settings;
using TaleWorlds.MountAndBlade;

namespace Retinues.Tests.Cases
{
    public static class RetinueHealthTests
    {
        [GameTest("IndomitableDoesNotBuffEnemyOrUnassignedRetinues", "doctrines", RequiresCampaign = false)]
        public static void IndomitableTeamScope()
        {
            using var engine = new Europe1700RecruitmentTests.Fixture();
            using var configured = TestConfig.Set(Configuration.RetinueHealthBonus, 25);
            DoctrinesRegistry.EnsureRegistered();
            var doctrine = DoctrineCatalog.Indomitable;
            bool wasAcquired = doctrine.IsAcquired;
            try
            {
                var retinue = engine.Wrap(engine.Troop("health_retinue", custom: true), TroopSourceFlags.Retinue);
                var ordinary = engine.Wrap(engine.Troop("health_regular"), TroopSourceFlags.None);
                var player = (Team)FormatterServices.GetUninitializedObject(typeof(Team));
                var enemy = (Team)FormatterServices.GetUninitializedObject(typeof(Team));

                doctrine.IsAcquired = true;
                Tests.AssertEqual(35, RetinueHealthBonusPatches.GetHealthBonus(retinue, 100, player, player));
                // The same troop definition can be fielded by either side: its identity
                // must not make an enemy eligible for the player's acquired doctrine.
                Tests.AssertEqual(25, RetinueHealthBonusPatches.GetHealthBonus(retinue, 100, enemy, player),
                    "Enemies keep the configured bonus but must not receive Indomitable.");
                Tests.AssertEqual(25, RetinueHealthBonusPatches.GetHealthBonus(retinue, 100, null, player));
                Tests.AssertEqual(25, RetinueHealthBonusPatches.GetHealthBonus(retinue, 100, null, null));
                Tests.AssertEqual(25, RetinueHealthBonusPatches.GetHealthBonus(retinue, 100, player, null));
                Tests.AssertEqual(0, RetinueHealthBonusPatches.GetHealthBonus(ordinary, 100, player, player));
                Tests.AssertEqual(0, RetinueHealthBonusPatches.GetHealthBonus(null, 100, player, player));

                doctrine.IsAcquired = false;
                Tests.AssertEqual(25, RetinueHealthBonusPatches.GetHealthBonus(retinue, 100, player, player));
                Tests.AssertEqual(25, RetinueHealthBonusPatches.GetHealthBonus(retinue, 100, enemy, player));

                using var noConfiguredBonus = TestConfig.Set(Configuration.RetinueHealthBonus, 0);
                Tests.AssertEqual(0, RetinueHealthBonusPatches.GetHealthBonus(retinue, 100, player, player));
                doctrine.IsAcquired = true;
                Tests.AssertEqual(10, RetinueHealthBonusPatches.GetHealthBonus(retinue, 100, player, player));
                Tests.AssertEqual(0, RetinueHealthBonusPatches.GetHealthBonus(retinue, 100, enemy, player));
                Tests.AssertEqual(11, RetinueHealthBonusPatches.GetHealthBonus(retinue, 105, player, player),
                    "Percentage rounding remains unchanged.");
            }
            finally { doctrine.IsAcquired = wasAcquired; }
        }
    }
}
