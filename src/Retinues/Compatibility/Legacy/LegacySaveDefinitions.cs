using System.Collections.Generic;
using Retinues.Compatibility.Legacy.Save;
using TaleWorlds.SaveSystem;

namespace Retinues.Compatibility.Legacy
{
    /// <summary>
    /// Legacy SaveableTypeDefiner for very old Retinues versions that used a different definer id.
    /// This exists solely so the TaleWorlds save system can deserialize old objects.
    /// </summary>
    public sealed class LegacySaveDefinitions : SaveableTypeDefiner
    {
        // From the legacy mod version's SaveDefinitions.
        private const int LegacyDefinerId = 090_787;

        /// <summary>
        /// Creates a definer for the legacy Retinues save schema.
        /// The definer id must match the one used by legacy saves so deserialization can resolve
        /// legacy class/type ids.
        /// </summary>
        public LegacySaveDefinitions()
            : base(LegacyDefinerId) { }

        /// <summary>
        /// Registers legacy saveable classes and their type ids.
        /// These ids must match the legacy schema exactly.
        /// </summary>
        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();
            AddClassDefinition(typeof(TroopSaveData), 070_910);
            AddClassDefinition(typeof(TroopBodySaveData), 070_911);
            AddClassDefinition(typeof(TroopEquipmentData), 070_912);
            AddClassDefinition(typeof(TroopSkillData), 070_913);

            AddClassDefinition(typeof(FactionSaveData), 070_920);
        }

        /// <summary>
        /// Registers legacy container definitions (lists used in SyncData payloads).
        /// </summary>
        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();
            ConstructContainerDefinition(typeof(List<TroopSaveData>));
            ConstructContainerDefinition(typeof(List<FactionSaveData>));
        }
    }
}
