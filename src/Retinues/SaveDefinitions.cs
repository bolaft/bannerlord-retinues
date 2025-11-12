using System.Collections.Generic;
using Retinues.Features.Equipments;
using Retinues.Features.Staging;
using Retinues.Safety.Legacy;
using Retinues.Troops.Save;
using TaleWorlds.SaveSystem;

namespace Retinues
{
    /// <summary>
    /// Defines saveable types and container definitions for the mod.
    /// </summary>
    public sealed class SaveDefinitions : SaveableTypeDefiner
    {
        /// <summary>
        /// Construct the definer with the module's unique base ID.
        /// </summary>
        public SaveDefinitions()
            : base(090_787) { }

        /// <summary>
        /// Register classes that the save system must know how to serialize.
        /// Include current types and legacy types needed to load older saves.
        /// </summary>
        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();

            // Troop save data
            AddClassDefinition(typeof(TroopSaveData), 070_910);
            AddClassDefinition(typeof(TroopBodySaveData), 070_911);
            AddClassDefinition(typeof(TroopEquipmentData), 070_912);
            AddClassDefinition(typeof(TroopSkillData), 070_913);

            // Faction save data
            AddClassDefinition(typeof(FactionSaveData), 070_920);

            // Staged operations data
            AddClassDefinition(typeof(PendingTrainData), 070_001);
            AddClassDefinition(typeof(PendingEquipData), 070_002);

            // Equipment set usage data
            AddClassDefinition(typeof(EquipmentPolicy), 200901);

            // Legacy data
            AddClassDefinition(typeof(LegacyTroopSaveData), 070_992);
        }

        /// <summary>
        /// Register container (collection) types used by saved data.
        /// Include legacy container signatures so older saves deserialize correctly.
        /// </summary>
        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();

            // Staged operations containers
            ConstructContainerDefinition(typeof(Dictionary<string, PendingTrainData>));
            ConstructContainerDefinition(typeof(Dictionary<string, PendingEquipData>));
            ConstructContainerDefinition(
                typeof(Dictionary<string, Dictionary<string, PendingTrainData>>)
            );
            ConstructContainerDefinition(
                typeof(Dictionary<string, Dictionary<string, PendingEquipData>>)
            );

            // Troop save data containers
            ConstructContainerDefinition(typeof(List<TroopSaveData>));
            ConstructContainerDefinition(typeof(List<string>));

            // Faction save data containers
            ConstructContainerDefinition(typeof(List<FactionSaveData>));

            // Combat equipment behavior containers
            ConstructContainerDefinition(typeof(Dictionary<int, byte>));
            ConstructContainerDefinition(typeof(Dictionary<string, Dictionary<int, byte>>));

            // Equipment set usage containers
            ConstructContainerDefinition(typeof(Dictionary<int, EquipmentPolicy>));
            ConstructContainerDefinition(
                typeof(Dictionary<string, Dictionary<int, EquipmentPolicy>>)
            );

            // Retinue hire containers
            ConstructContainerDefinition(typeof(Dictionary<string, int>));

            // Legacy containers
            ConstructContainerDefinition(typeof(List<LegacyTroopSaveData>));
        }
    }
}
