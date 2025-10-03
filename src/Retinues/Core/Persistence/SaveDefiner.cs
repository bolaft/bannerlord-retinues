using System.Collections.Generic;
using Retinues.Core.Persistence.Troop;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Persistence
{
    public sealed class SaveDefiner : SaveableTypeDefiner
    {
        public SaveDefiner()
            : base(090_787) { }

        protected override void DefineClassTypes()
        {
            base.DefineContainerDefinitions();

            AddClassDefinition(typeof(TroopSaveData), 070_992);
        }

        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();

            ConstructContainerDefinition(typeof(List<TroopSaveData>));
        }
    }
}
