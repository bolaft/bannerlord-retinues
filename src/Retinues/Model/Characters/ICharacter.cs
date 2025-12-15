using System.Collections.Generic;
using Retinues.Model.Equipments;
using Retinues.Model.Factions;
using TaleWorlds.Core;

namespace Retinues.Model.Characters
{
    public interface IEditableUnit
    {
        string Name { get; set; }
        int Level { get; set; }
        bool IsFemale { get; set; }
        WCulture Culture { get; set; }
        IEditableSkills Skills { get; }
        List<MEquipment> Equipments { get; }
    }

    public interface IEditableSkills
    {
        int Get(SkillObject skill);
        void Set(SkillObject skill, int value);
        void Modify(SkillObject skill, int amount);
    }
}
