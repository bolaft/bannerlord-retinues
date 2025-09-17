using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Persistence.Troop
{
    public class TroopSaveData
    {
        [SaveableField(1)] public string VanillaStringId;
        [SaveableField(2)] public bool IsKingdomTroop;
        [SaveableField(3)] public bool IsElite;
        [SaveableField(4)] public bool IsEliteRetinue;
        [SaveableField(5)] public bool IsBasicRetinue;
        [SaveableField(6)] public string StringId;
        [SaveableField(7)] public string Name;
        [SaveableField(8)] public int Level;
        [SaveableField(9)] public bool IsFemale;
        [SaveableField(10)] public string CultureId;
        [SaveableField(11)] public string SkillCode;
        [SaveableField(12)] public string EquipmentCode;
        [SaveableField(13)] public List<TroopSaveData> UpgradeTargets = [];
    }
}
