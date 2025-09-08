using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Editor.UI.VM.Troop
{
    public sealed class TroopSkillVM(SkillObject skill, WCharacter troop, TroopEditorVM editor) : ViewModel, IView
    {
        // =========================================================================
        // Fields
        // =========================================================================

        readonly SkillObject _skill = skill;

        readonly WCharacter _troop = troop;

        readonly TroopEditorVM _editor = editor;

        // =========================================================================
        // Data Bindings
        // =========================================================================

        [DataSourceProperty]
        public int Value => _troop.GetSkill(_skill);

        [DataSourceProperty]
        public string StringId => _skill.StringId;

        [DataSourceProperty]
        public bool CanIncrement => TroopRules.CanIncrementSkill(_troop, _skill);

        [DataSourceProperty]
        public bool CanDecrement => TroopRules.CanDecrementSkill(_troop, _skill);

        // =========================================================================
        // Action Bindings
        // =========================================================================

        [DataSourceMethod]
        public void ExecuteIncrement() => Modify(+1);

        [DataSourceMethod]
        public void ExecuteDecrement() => Modify(-1);

        // =========================================================================
        // Public API
        // =========================================================================

        public void Refresh()
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(StringId));
            OnPropertyChanged(nameof(CanIncrement));
            OnPropertyChanged(nameof(CanDecrement));
        }

        // =========================================================================
        // Internals
        // =========================================================================

        private void Modify(int delta)
        {
            int repeat = 1;

            if (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
                repeat = 500; // Ctrl = 500
            else if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
                repeat = 5; // Shift = 5

            for (int i = 0; i < repeat; i++)
            {
                if (delta > 0 && !CanIncrement) break;
                if (delta < 0 && !CanDecrement) break;

                TroopManager.ModifySkill(_troop, _skill, delta);
            }

            // Refresh value
            OnPropertyChanged(nameof(Value));

            // Refresh editor counters
            _editor.OnPropertyChanged(nameof(_editor.SkillTotal));
            _editor.OnPropertyChanged(nameof(_editor.SkillPointsUsed));

            // Refresh all skills buttons
            foreach (var s in _editor.SkillsRow1.Concat(_editor.SkillsRow2))
            {
                s.OnPropertyChanged(nameof(s.CanIncrement));
                s.OnPropertyChanged(nameof(s.CanDecrement));
            }
        }
    }
}
