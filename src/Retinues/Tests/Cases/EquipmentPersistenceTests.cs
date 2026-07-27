using System.Linq;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for equipment set round-tripping through troop serialization (the save/export/import
    /// payload path). Equipment codes carry items only — the battle/civilian TYPE of each set is
    /// restored solely from its persisted IsCivilianAttribute. If that flag is lost, the troop ends
    /// up with no battle-typed set and the engine silently renders/spawns the first (civilian) set,
    /// which is how imported troops showed up in civilian clothes in previews.
    /// </summary>
    public static class EquipmentPersistenceTests
    {
        [GameTest(
            "EquipmentTypesRoundTrip",
            "equipment",
            "Battle/civilian set types and items survive serialize -> rebuild -> deserialize"
        )]
        public static void EquipmentTypesRoundTrip(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var body = WItem.GetEquipmentsForSlot(EquipmentIndex.Body)?.FirstOrDefault(i =>
                i?.Base != null
            );
            Tests.AssertNotNull(body, "A body item exists to equip.");

            var wc = sandbox.NewStub();

            var battle = MEquipment.Create(wc, civilian: false);
            battle.Set(EquipmentIndex.Body, body);
            var civilian = MEquipment.Create(wc, civilian: true);

            wc.EquipmentRoster.Equipments = [battle, civilian];

            var saved = wc.Serialize();

            // Sabotage the live state so the restore has to rebuild everything from the payload.
            wc.EquipmentRoster.Equipments = [MEquipment.Create(wc, civilian: true)];

            wc.Deserialize(saved);
            wc.InvalidateEquipmentRosterCache();

            var sets = wc.EquipmentRoster.Equipments;
            Tests.AssertEqual(2, sets.Count, "Both sets were restored.");
            Tests.AssertFalse(sets[0].IsCivilian, "First set is battle-typed after restore.");
            Tests.AssertTrue(sets[1].IsCivilian, "Second set is civilian-typed after restore.");

            // The engine-level lookup previews and spawns rely on.
            Tests.AssertNotNull(
                wc.Base.FirstBattleEquipment,
                "The engine sees a battle-typed set after restore."
            );
            Tests.AssertEqual(
                body.StringId,
                sets[0].Get(EquipmentIndex.Body)?.StringId,
                "The battle set's body armor survived the round trip."
            );
        }

        [GameTest(
            "AllCivilianRestoreSelfHeals",
            "equipment",
            "A restore that yields only civilian sets re-types the first set as battle"
        )]
        public static void AllCivilianRestoreSelfHeals(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var wc = sandbox.NewStub();

            // Simulate a corrupted payload: every set flagged civilian, no battle set anywhere.
            wc.EquipmentRoster.Equipments =
            [
                MEquipment.Create(wc, civilian: true),
                MEquipment.Create(wc, civilian: true),
            ];

            var saved = wc.Serialize();
            wc.Deserialize(saved);
            wc.InvalidateEquipmentRosterCache();

            Tests.AssertNotNull(
                wc.Base.FirstBattleEquipment,
                "After restore, the troop always has at least one battle-typed set (self-heal)."
            );
            Tests.AssertFalse(
                wc.EquipmentRoster.Equipments[0].IsCivilian,
                "The first set was re-typed as battle."
            );
        }
    }
}
