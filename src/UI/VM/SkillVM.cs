using System;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class SkillVM : ViewModel
    {
        private TroopEditorVM _owner;

        private CharacterWrapper _troop => _owner.Troop;

        private SkillObject _skill;

        public SkillVM(SkillObject skill, TroopEditorVM owner)
        {
            _owner = owner;
            _skill = skill;
        }

        [DataSourceProperty] public int Value => _troop.GetSkill(_skill);

        [DataSourceProperty] public string StringId => _skill.StringId;

        [DataSourceProperty] public string Name => _skill.Name.ToString();

        [DataSourceProperty] public bool CanIncrement
        {
            get
            {
                // At tier skill cap
                if (Value >= _troop.SkillCap) return false;
                // No skill points left to spend
                if (_troop.Skills.Values.Sum() >= _troop.SkillPoints) return false;

                return true;
            }
        }

        [DataSourceProperty] public bool CanDecrement
        {
            get
            {
                // Can't go below zero
                if (Value <= 0) return false;

                // At minimal threshold for equipped gear
                if (Value <= _troop.GetMinimumSkillRequirement(_skill)) return false;

                return true;
            }
        }

            [DataSourceMethod]
        public void ExecuteIncrement() => Modify(+1);

        [DataSourceMethod]
        public void ExecuteDecrement() => Modify(-1);

        private void Modify(int delta)
        {
            if (_troop == null || _skill == null) return;

            int repeat = 1;

            if (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
                repeat = 500;
            else if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
                repeat = 5;

            for (int i = 0; i < repeat; i++)
            {
                if (delta > 0 && !CanIncrement) break;
                if (delta < 0 && !CanDecrement) break;

                _troop.SetSkill(_skill, _troop.GetSkill(_skill) + delta);
            }

            Refresh();
        }

        public void Refresh()
        {
            // Refresh skill points used
            _owner.Refresh();

            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(StringId));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(CanIncrement));
            OnPropertyChanged(nameof(CanDecrement));
        }
    }
}