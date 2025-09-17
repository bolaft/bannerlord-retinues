using System.Collections.Generic;
using Retinues.Core.Persistence.Item;
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

            AddClassDefinition(typeof(RosterElementSaveData), 070_990);
            AddClassDefinition(typeof(RosterSaveData), 070_991);
            AddClassDefinition(typeof(TroopSaveData), 070_992);
            AddClassDefinition(typeof(ItemSaveData), 070_993);
        }

        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();

            ConstructContainerDefinition(typeof(List<RosterElementSaveData>));
            ConstructContainerDefinition(typeof(List<RosterSaveData>));
            ConstructContainerDefinition(typeof(List<TroopSaveData>));
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(List<string>));
            ConstructContainerDefinition(typeof(List<int>));
        }
    }
}
