using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Persistence.Troop
{
    public class RosterSaveData
    {
        [SaveableField(1)] public string PartyId;
        [SaveableField(2)] public bool IsPrisonRoster;
        [SaveableField(3)] public List<RosterElementSaveData> Elements = [];
    }

    public class RosterElementSaveData
    {
        [SaveableField(1)] public int Healthy;
        [SaveableField(2)] public int Wounded;
        [SaveableField(3)] public int Xp;
        [SaveableField(4)] public bool IsKingdom;
        [SaveableField(5)] public bool IsRetinue;
        [SaveableField(6)] public bool IsElite;
        [SaveableField(7)] public List<int> PositionInTree = [];
        [SaveableField(8)] public int Index;
    }
}
