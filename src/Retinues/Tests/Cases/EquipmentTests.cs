using Retinues.Domain.Characters.Services.Cloning;
using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.CampaignSystem;
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
    }
}
