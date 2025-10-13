using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop
{
    /// <summary>
    /// ViewModel for a troop skill. Handles increment/decrement logic, retraining warnings, and UI refresh.
    /// </summary>
    [SafeClass]
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
        public int DisplayValue => Value + StagedPoints;

        [DataSourceProperty]
        public bool IsStaged => Staged != null && StagedPoints > 0;

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
        public void ExecuteIncrement() => Modify(true);

        [DataSourceMethod]
        public void ExecuteDecrement() => Modify(false);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void Refresh()
        {
            OnPropertyChanged(nameof(DisplayValue));
            OnPropertyChanged(nameof(IsStaged));
            OnPropertyChanged(nameof(StringId));
            OnPropertyChanged(nameof(CanIncrement));
            OnPropertyChanged(nameof(CanDecrement));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private int Value => _troop?.GetSkill(_skill) ?? 0;
        private int StagedPoints => Staged?.PointsRemaining ?? 0;
        private PendingTrainData Staged =>
            TroopTrainBehavior.Instance?.GetPending(_troop.StringId, _skill.StringId) ?? null;

        private void Modify(bool increment)
        {
            if (!Config.TrainingTakesTime && increment) // Allow edits since you need a settlement to train anyway
                if (_editor.Screen?.EditingIsAllowed == false)
                    return; // Editing not allowed in current context

            int repeat = 1;

            if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
                repeat = 5; // Shift = 5

            void doModify()
            {
                for (int i = 0; i < repeat; i++)
                {
                    if (increment && !CanIncrement)
                        break;
                    if (!increment && !CanDecrement)
                        break;

                    TroopManager.ModifySkill(_troop, _skill, increment);
                }

                // Refresh value
                OnPropertyChanged(nameof(DisplayValue));
                OnPropertyChanged(nameof(IsStaged));

                // Refresh editor counters
                _editor.OnPropertyChanged(nameof(_editor.SkillTotal));
                _editor.OnPropertyChanged(nameof(_editor.SkillPointsUsed));
                _editor.OnPropertyChanged(nameof(_editor.AvailableTroopXpText));
                _editor.OnPropertyChanged(nameof(_editor.TrainingRequired));
                _editor.OnPropertyChanged(nameof(_editor.TrainingRequiredText));
                _editor.OnPropertyChanged(nameof(_editor.TrainingIsRequired));
                _editor.OnPropertyChanged(nameof(_editor.NoTrainingIsRequired));

                // Refresh all skills buttons
                foreach (var s in _editor.SkillsRow1.Concat(_editor.SkillsRow2))
                {
                    s.OnPropertyChanged(nameof(s.CanIncrement));
                    s.OnPropertyChanged(nameof(s.CanDecrement));
                }
            }

            if (
                !DoctrineAPI.IsDoctrineUnlocked<AdaptiveTraining>()
                && !increment
                && !IsStaged
                && !_editor.PlayerWarnedAboutRetraining
                && (Config.BaseSkillXpCost + Config.SkillXpCostPerPoint) > 0
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
