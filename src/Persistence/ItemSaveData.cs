using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace CustomClanTroops.Persistence
{
    public class ItemSaveData
    {
        [SaveableField(1)] public List<string> UnlockedItemIds = [];
        [SaveableField(2)] public Dictionary<string, int> StockedItems = [];
    }
}