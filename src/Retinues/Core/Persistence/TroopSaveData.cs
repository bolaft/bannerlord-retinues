using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Persistence
{
    public class TroopSaveData
    {
        [SaveableField(1)] public string VanillaStringId;
        [SaveableField(2)] public bool IsKingdomTroop;
        [SaveableField(3)] public bool IsElite;
        [SaveableField(4)] public string StringId;
        [SaveableField(5)] public string Name;
        [SaveableField(6)] public int Level;
        [SaveableField(7)] public bool IsFemale;
        [SaveableField(8)] public string CultureId;
        [SaveableField(9)] public string SkillCode;
        [SaveableField(10)] public string EquipmentCode;
        [SaveableField(11)] public List<TroopSaveData> UpgradeTargets = [];
    }
}