using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Wrappers;
using Retinues.Troops.Edition;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    [SafeClass]
    public sealed class TroopSkillVM : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly TroopScreenVM Screen;
        public readonly SkillObject Skill;

        public TroopSkillVM(TroopScreenVM screen, SkillObject skill)
        {
            Log.Info("Building TroopSkillVM...");

            Screen = screen;
            Skill = skill;
        }

        public void Initialize()
        {
            Log.Info("Initializing TroopSkillVM...");

            // Subscribe to events
            void Refresh()
            {
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(ValueColor));
                OnPropertyChanged(nameof(IsStaged));
                OnPropertyChanged(nameof(CanIncrement));
                OnPropertyChanged(nameof(CanDecrement));
            }
            EventManager.SkillChange.Register(Refresh);
            EventManager.TroopChange.Register(Refresh);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WCharacter SelectedTroop => Screen?.TroopList?.Selection?.Troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public int Value => (SelectedTroop?.GetSkill(Skill) ?? 0) + StagedAmount;

        [DataSourceProperty]
        public string ValueColor => IsStaged ? "#ebaf2fff" : "#F4E1C4FF";

        [DataSourceProperty]
        public bool IsStaged => StagedAmount > 0;

        [DataSourceProperty]
        public string SkillId => Skill.StringId;

        [DataSourceProperty]
        public bool CanIncrement => TroopRules.CanIncrementSkill(SelectedTroop, Skill);

        [DataSourceProperty]
        public bool CanDecrement => TroopRules.CanDecrementSkill(SelectedTroop, Skill);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteIncrement() => Modify(true);

        [DataSourceMethod]
        public void ExecuteDecrement() => Modify(false);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Staging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private int StagedAmount =>
            TroopTrainBehavior
                .Instance.GetStagedChange(SelectedTroop, Skill?.StringId)
                ?.PointsRemaining
            ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool PlayerWarnedAboutRetraining = false;

        private void Modify(bool increment)
        {
            if (Config.TrainingTakesTime == false) // Only check in instant training mode
                if (
                    TroopRules.IsAllowedInContextWithPopup(
                        SelectedTroop,
                        L.S("action_modify", "modify")
                    ) == false
                )
                    return; // Modification not allowed in current context

            // Local function to perform the actual modification
            void DoModify()
            {
                for (int i = 0; i < BatchInput(); i++)
                {
                    if (increment == true && !CanIncrement)
                        break; // Can't increment further
                    if (increment == false && !CanDecrement)
                        break; // Can't decrement further

                    TroopManager.ModifySkill(SelectedTroop, Skill, increment);
                }

                EventManager.SkillChange.Fire();
            }

            // Warn the player if decrementing a skill may require retraining
            if (
                !DoctrineAPI.IsDoctrineUnlocked<AdaptiveTraining>() // No warning if Adaptive Training is unlocked
                && !increment // Only warn on decrement
                && !IsStaged // No warning if removing staged points
                && !PlayerWarnedAboutRetraining // No warning if already warned this session
                && (Config.BaseSkillXpCost + Config.SkillXpCostPerPoint) > 0 // No warning if skills are free
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
                            DoModify(); // Proceed with modification
                            PlayerWarnedAboutRetraining = true;
                        },
                        negativeAction: () => { }
                    )
                );
            }
            else
            {
                DoModify(); // No warning needed, proceed with modification
            }
        }
    }
}
