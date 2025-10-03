using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Troops.Save
{
    public sealed class TroopSaveDefiner : SaveableTypeDefiner
    {
        public TroopSaveDefiner()
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
