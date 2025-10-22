using System.Collections.Generic;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Features.Missions.Behaviors;
using Retinues.Troops.Persistence;
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
            AddClassDefinition(typeof(TroopSaveData), 070_992);

            // Staged operations data
            AddClassDefinition(typeof(PendingTrainData), 070_001);
            AddClassDefinition(typeof(PendingEquipData), 070_002);

            // Equipment set usage data
            AddClassDefinition(typeof(EquipmentUsePolicy), 200901);

            // Legacy definitions for backwards compatibility
            AddClassDefinition(typeof(Safety.Legacy.Behaviors.ItemSaveData), 070_993);
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

            // Combat equipment behavior containers
            ConstructContainerDefinition(typeof(Dictionary<int, byte>));
            ConstructContainerDefinition(typeof(Dictionary<string, Dictionary<int, byte>>));

            // Equipment set usage containers
            ConstructContainerDefinition(typeof(Dictionary<int, EquipmentUsePolicy>));
            ConstructContainerDefinition(typeof(Dictionary<string, Dictionary<int, EquipmentUsePolicy>>));

            // Retinue hire containers
            ConstructContainerDefinition(typeof(Dictionary<string, int>));

            // Legacy containers for backwards compatibility
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(List<int>));
        }
    }
}
