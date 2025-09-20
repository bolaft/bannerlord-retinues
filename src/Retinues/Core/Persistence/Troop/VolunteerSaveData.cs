using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Persistence.Troop
{
    public class VolunteerSaveData
    {
        [SaveableField(1)] public string SettlementId;
        [SaveableField(2)] public string NotableId;
        [SaveableField(3)] public List<VolunteerSlotSaveData> Slots = [];
    }

    public class VolunteerSlotSaveData
    {
        [SaveableField(1)] public int Index;
        [SaveableField(2)] public bool IsKingdom;
        [SaveableField(3)] public bool IsRetinue;
        [SaveableField(4)] public bool IsElite;
        [SaveableField(5)] public List<int> PositionInTree = [];
    }
}
