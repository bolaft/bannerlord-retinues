using System.Collections.Generic;
using TaleWorlds.SaveSystem;
using CustomClanTroops.Persistence;

namespace CustomClanTroops.Persistence
{
    // This class is discovered automatically by the save system when the assembly loads.
    public sealed class SaveDefiner : SaveableTypeDefiner
    {
        // This base id is just a namespace for your type ids; pick a unique-ish int and keep it forever.
        public SaveDefiner() : base(090_787) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(TroopSaveData), 070_992);
            AddClassDefinition(typeof(ItemSaveData), 070_993);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<TroopSaveData>));
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
            ConstructContainerDefinition(typeof(List<string>));
        }
    }
}
