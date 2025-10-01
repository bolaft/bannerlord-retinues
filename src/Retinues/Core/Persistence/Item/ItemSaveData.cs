using System.Collections.Generic;
using Retinues.Core.Utils;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Persistence.Item
{
    [SafeClass(SwallowByDefault = false)]
    public class ItemSaveData
    {
        [SaveableField(1)]
        public List<string> UnlockedItemIds = [];

        [SaveableField(2)]
        public Dictionary<string, int> StockedItems = [];
    }
}
