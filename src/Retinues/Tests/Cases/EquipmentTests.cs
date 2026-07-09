using System.Linq;
using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Equipments.Services.Random;
using Retinues.Domain.Equipments.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>Tests for the per-equipment-set battle-context policy (field / siege / naval).</summary>
    public static class EquipmentTests
    {
        [GameTest(
            "EquipmentSetTogglesAndCivilian",
            "equipment",
            "Per-set battle toggles flip; civilian sets always count as field sets"
        )]
        public static void EquipmentSetTogglesAndCivilian(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var looter = MBObjectManager.Instance.GetObject<CharacterObject>("looter");
            Tests.AssertNotNull(looter, "A vanilla 'looter' troop exists to clone from.");

            var clone = sandbox.Track(CharacterCloner.Clone(WCharacter.Get(looter)));
            var sets = clone.EquipmentRoster.Equipments;
            Tests.AssertTrue(sets.Count > 0, "Cloned troop has at least one equipment set.");

            var eq = sets[0];
            eq.IsCivilian = false;

            eq.FieldBattleSet = false;
            Tests.AssertFalse(eq.FieldBattleSet, "FieldBattleSet toggles off on a battle set.");
            eq.FieldBattleSet = true;
            Tests.AssertTrue(eq.FieldBattleSet, "FieldBattleSet toggles back on.");

            eq.SiegeBattleSet = false;
            Tests.AssertFalse(eq.SiegeBattleSet, "SiegeBattleSet toggles off.");

            // A civilian set is always a field set regardless of the stored flag.
            eq.IsCivilian = true;
            Tests.AssertTrue(eq.FieldBattleSet, "Civilian set always counts as a field set.");
        }

        [GameTest(
            "RandomEquipmentNeverTwoShields",
            "equipment",
            "Randomizing a two-shield source never produces a troop carrying two shields"
        )]
        public static void RandomEquipmentNeverTwoShields(GameTestContext ctx)
        {
            ctx.EnsureCampaign();
            using var sandbox = new TestSandbox();

            var shields = WItem.GetEquipmentsForSlot(EquipmentIndex.Weapon0)
                ?.Where(i => i != null && i.IsShield)
                .Take(2)
                .ToList();

            if (shields == null || shields.Count < 2)
                return; // This load order has too few shields to exercise the case; nothing to assert.

            var owner = sandbox.NewStub();
            owner.Level = 20; // high tier so the randomizer can actually pick the shields

            // Build a deliberately-bad source: two shields in the weapon slots.
            var source = MEquipment.Create(owner, civilian: false, source: null);
            source.Set(EquipmentIndex.Weapon0, shields[0]);
            source.Set(EquipmentIndex.Weapon1, shields[1]);

            var result = EquipmentRandomizer.CreateRandomEquipment(
                owner: owner,
                source: source,
                civilian: false,
                requireSkillForItem: false
            );

            Tests.AssertNotNull(result, "Randomizer produced an equipment set.");

            int shieldCount = 0;
            foreach (
                var slot in new[]
                {
                    EquipmentIndex.Weapon0,
                    EquipmentIndex.Weapon1,
                    EquipmentIndex.Weapon2,
                    EquipmentIndex.Weapon3,
                }
            )
            {
                var it = result.Get(slot);
                if (it != null && it.IsShield)
                    shieldCount++;
            }

            Tests.AssertTrue(
                shieldCount <= 1,
                $"A troop is never given more than one shield (got {shieldCount})."
            );
        }
    }
}
