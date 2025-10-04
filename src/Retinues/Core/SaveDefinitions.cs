using System.Collections.Generic;
using Retinues.Core.Troops.Save;
using TaleWorlds.SaveSystem;

namespace Retinues.Core
{
    public sealed class SaveDefinitions : SaveableTypeDefiner
    {
        public SaveDefinitions()
            : base(090_787) { }

        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();

            AddClassDefinition(typeof(TroopSaveData), 070_992);

            // Legacy definitions for backwards compatibility
            AddClassDefinition(typeof(Safety.Legacy.Behaviors.ItemSaveData), 070_993);
        }

        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();

            ConstructContainerDefinition(typeof(List<TroopSaveData>));
            ConstructContainerDefinition(typeof(List<string>));

            // Legacy containers for backwards compatibility
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(List<int>));
        }
    }
}
