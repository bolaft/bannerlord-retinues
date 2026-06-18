using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.Troops.Save;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Round-trip tests for troop serialization. These guard the save/load layer where the
    /// revolt-corruption bug lived: a serialized troop must deserialize back to identical state.
    /// </summary>
    public static class SaveTests
    {
        /// <summary>
        /// Serialize a troop, mutate the live object, then deserialize and assert the saved
        /// state is fully restored (name, level, skills, equipment). Also asserts that the
        /// faction-less deserialize path re-registers the stub as active (the fix-3 invariant).
        /// </summary>
        [GameTest(
            "TroopSaveRoundTrip",
            "save",
            "TroopSaveData serialize -> deserialize restores name, level, skills, equipment"
        )]
        public static void TroopSaveRoundTrip(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var vanilla = Player.Clan?.Culture?.RootBasic;
            Tests.AssertNotNull(vanilla, "Player culture has a basic root troop to clone from.");

            using var sandbox = new TestSandbox();

            // Throwaway custom troop on a free stub, not bound to any real faction root.
            var troop = sandbox.NewStub();
            troop.FillFrom(vanilla, keepUpgrades: false, keepEquipment: true, keepSkills: true);
            troop.Name = "RetinuesTestTroop";
            troop.Level = 17;

            // Capture expected state.
            var expectedName = troop.Name;
            var expectedLevel = troop.Level;
            var expectedSkills = troop.Skills.ToDictionary(kv => kv.Key, kv => kv.Value);
            var expectedCodes = troop.Loadout.Equipments.Select(e => e.Code).ToList();
            var stubId = troop.StringId;

            // Serialize, then mutate the live troop to prove the restore actually happens.
            var data = new TroopSaveData(troop);
            troop.Name = "MUTATED";
            troop.Level = 1;

            // Faction-less deserialize writes back onto the same stub id.
            var rebuilt = data.Deserialize();
            Tests.AssertNotNull(rebuilt, "Deserialize produced a troop.");

            Tests.AssertEqual(expectedName, rebuilt.Name, "Name round-trips.");
            Tests.AssertEqual(expectedLevel, rebuilt.Level, "Level round-trips.");

            foreach (var kv in expectedSkills)
            {
                Tests.AssertEqual(
                    kv.Value,
                    rebuilt.GetSkill(kv.Key),
                    $"Skill '{kv.Key.StringId}' round-trips."
                );
            }

            var rebuiltCodes = rebuilt.Loadout.Equipments.Select(e => e.Code).ToList();
            Tests.AssertEqual(
                string.Join("|", expectedCodes),
                string.Join("|", rebuiltCodes),
                "Equipment codes round-trip."
            );

            // Fix-3: the faction-less deserialize path must claim the stub.
            Tests.AssertTrue(
                WCharacter.ActiveStubIds.Contains(stubId),
                "Deserialize registered the stub as active (stub-recycling guard)."
            );
        }
    }
}
