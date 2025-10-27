using System.Collections.Generic;
using Retinues.Features.Missions.Behaviors;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Troops.Save;
using TaleWorlds.SaveSystem;

namespace MudToMail
{
    /// <summary>
    /// Defines saveable types and container definitions for the mod.
    /// </summary>
    public sealed class SaveDefinitions : SaveableTypeDefiner
    {
        /// <summary>
        /// Construct the definer with the module's unique base ID.
        /// </summary>
        public SaveDefinitions() : base(070_992) { }

        /// <summary>
        /// Register classes that the save system must know how to serialize.
        /// Include current types and legacy types needed to load older saves.
        /// </summary>
        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();
        }

        /// <summary>
        /// Register container (collection) types used by saved data.
        /// Include legacy container signatures so older saves deserialize correctly.
        /// </summary>
        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();
        }
    }
}
