using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Features.Doctrines;
using Retinues.Core.Features.Doctrines.Catalog;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Troop
{
    public sealed class TroopSkillVM(SkillObject skill, WCharacter troop, TroopEditorVM editor)
        : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        readonly SkillObject _skill = skill;

        readonly WCharacter _troop = troop;

        readonly TroopEditorVM _editor = editor;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public int Value => _troop.GetSkill(_skill);

        [DataSourceProperty]
        public string StringId => _skill.StringId;

        [DataSourceProperty]
        public bool CanIncrement => TroopRules.CanIncrementSkill(_troop, _skill);

        [DataSourceProperty]
        public bool CanDecrement => TroopRules.CanDecrementSkill(_troop, _skill);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteIncrement() => Modify(+1);

        [DataSourceMethod]
        public void ExecuteDecrement() => Modify(-1);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void Refresh()
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(StringId));
            OnPropertyChanged(nameof(CanIncrement));
            OnPropertyChanged(nameof(CanDecrement));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void Modify(int delta)
        {
            int repeat = 1;

            if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
                repeat = 5; // Shift = 5

            void doModify()
            {
                for (int i = 0; i < repeat; i++)
                {
                    if (delta > 0 && !CanIncrement)
                        break;
                    if (delta < 0 && !CanDecrement)
                        break;

                    TroopManager.ModifySkill(_troop, _skill, delta);
                }

                // Refresh value
                OnPropertyChanged(nameof(Value));

                // Refresh editor counters
                _editor.OnPropertyChanged(nameof(_editor.SkillTotal));
                _editor.OnPropertyChanged(nameof(_editor.SkillPointsUsed));
                _editor.OnPropertyChanged(nameof(_editor.AvailableTroopXpText));

                // Refresh all skills buttons
                foreach (var s in _editor.SkillsRow1.Concat(_editor.SkillsRow2))
                {
                    s.OnPropertyChanged(nameof(s.CanIncrement));
                    s.OnPropertyChanged(nameof(s.CanDecrement));
                }
            }

            // TODO: add a check for doctrine once retraining refunds are unlocked
            if (
                !DoctrineAPI.IsDoctrineUnlocked<AdaptiveTraining>()
                && delta < 0
                && !_editor.PlayerWarnedAboutRetraining
                && Config.GetOption<int>("BaseSkillXpCost")
                    + Config.GetOption<int>("SkillXpCostPerPoint")
                    > 0
            )
            {
                // Warn the player that decrementing skills may require retraining
                InformationManager.ShowInquiry(
                    new InquiryData(
                        titleText: L.S("warning", "Warning"),
                        text: L.S(
                            "lower_skill_no_refund",
                            "Lowering this troop's skill will not refund any experience points. Continue anyway?"
                        ),
                        isAffirmativeOptionShown: true,
                        isNegativeOptionShown: true,
                        affirmativeText: L.S("continue", "Continue"),
                        negativeText: L.S("cancel", "Cancel"),
                        affirmativeAction: () =>
                        {
                            doModify();
                            _editor.PlayerWarnedAboutRetraining = true;
                        },
                        negativeAction: () => { }
                    )
                );
            }
            else
            {
                doModify();
            }
        }
    }
}
