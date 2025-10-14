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
    public sealed class TroopSkillVM(WCharacter troop, SkillObject skill) : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        readonly SkillObject _skill = skill;
        readonly WCharacter _troop = troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public int Value => _troop?.GetSkill(_skill) + StagedAmount ?? 0;

        [DataSourceProperty]
        public bool IsStaged => StagedAmount > 0;

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
        //                         Staging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private int StagedAmount =>
            TroopTrainBehavior
                .Instance.GetPending(_troop.StringId, _skill.StringId)
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
                        _troop,
                        Editor.Faction,
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

                    TroopManager.ModifySkill(_troop, _skill, increment);
                }
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
