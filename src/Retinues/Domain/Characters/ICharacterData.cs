using System.Collections.Generic;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Factions.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters
{
    public interface ICharacterData
    {
        string Name { get; set; }
        int Level { get; set; }
        bool IsFemale { get; set; }
        WCulture Culture { get; set; }
        ICharacterSkills Skills { get; }
        List<MEquipment> Equipments { get; }
    }

    public interface ICharacterSkills
    {
        int Get(SkillObject skill);
        void Set(SkillObject skill, int value);
        void Modify(SkillObject skill, int amount);
    }
}
