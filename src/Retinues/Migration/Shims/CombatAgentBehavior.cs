using System.Collections.Generic;
using Retinues.Migration.Legacy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

// Class name MUST stay "CombatAgentBehavior" – BL matches saved partition by class
// name (GetType().Name), so renaming would break loading legacy saves.
// ReSharper disable once CheckNamespace
namespace Retinues.Migration.Shims
{
    /// <summary>
    /// Read-only shim for v1 <c>CombatAgentBehavior</c>. Deserializes the per-troop,
    /// per-equipment-set usage policy (which alternate set is allowed in field / siege / naval).
    /// </summary>
    internal sealed class CombatAgentBehavior : CampaignBehaviorBase
    {
        // troop StringId → (equipment-set index → policy)
        internal Dictionary<string, Dictionary<int, EquipmentPolicy>> ByTroop;

        public override void RegisterEvents() { /* read-only shim */ }

        public override void SyncData(IDataStore dataStore)
        {
            // Read-only shim: never write legacy data back (keeps migration idempotent).
            if (dataStore.IsSaving)
                return;

            dataStore.SyncData("Retinues_EquipmentUsePolicy", ref ByTroop);
        }
    }
}
