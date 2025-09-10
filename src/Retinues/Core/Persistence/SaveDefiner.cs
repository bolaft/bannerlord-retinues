using System.Collections.Generic;
using TaleWorlds.SaveSystem;
using Retinues.Core.Persistence.Troop;
using Retinues.Core.Persistence.Item;

namespace Retinues.Core.Persistence
{
    public sealed class SaveDefiner : SaveableTypeDefiner
    {
        public SaveDefiner() : base(090_787) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(RosterSaveData), 070_991);
            AddClassDefinition(typeof(TroopSaveData), 070_992);
            AddClassDefinition(typeof(ItemSaveData), 070_993);
        }

        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();

            ConstructContainerDefinition(typeof(List<RosterSaveData>));
            ConstructContainerDefinition(typeof(List<TroopSaveData>));
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(List<string>));
        }
    }
}
