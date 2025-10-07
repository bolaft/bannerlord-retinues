using System.Collections.Generic;
using Retinues.Core.Features.Upgrade.Behaviors;
using Retinues.Core.Troops.Save;
using TaleWorlds.SaveSystem;

namespace Retinues.Core
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

            AddClassDefinition(typeof(TroopSaveData), 070_992);
            AddClassDefinition(typeof(TroopTrainingBehavior.PendingSave), 070_001);

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

            ConstructContainerDefinition(typeof(List<TroopTrainingBehavior.PendingSave>));
            ConstructContainerDefinition(typeof(List<TroopSaveData>));
            ConstructContainerDefinition(typeof(List<string>));

            // Legacy containers for backwards compatibility
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(List<int>));
        }
    }
}
