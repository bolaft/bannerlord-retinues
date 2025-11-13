using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Doctrines;
using Retinues.Doctrines.Catalog;
using Retinues.GUI.Helpers;
using Retinues.Managers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.GUI.Editor.VM.Troop.Panel
{
    /// <summary>
    /// ViewModel for a single troop skill, exposing value, staged state and controls.
    /// </summary>
    [SafeClass]
    public sealed class TroopSkillVM(SkillObject skill) : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly SkillObject Skill = skill;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Troop] =
                [
                    nameof(Value),
                    nameof(ValueColor),
                    nameof(IsStaged),
                    nameof(CanIncrement),
                    nameof(CanDecrement),
                    nameof(IncrementHint),
                    nameof(DecrementHint),
                ],
                [UIEvent.Train] =
                [
                    nameof(Value),
                    nameof(ValueColor),
                    nameof(IsStaged),
                    nameof(CanIncrement),
                    nameof(CanDecrement),
                    nameof(IncrementHint),
                    nameof(DecrementHint),
                ],
                [UIEvent.Equip] =
                [
                    nameof(CanIncrement),
                    nameof(CanDecrement),
                    nameof(IncrementHint),
                    nameof(DecrementHint),
                ],
                [UIEvent.Equipment] =
                [
                    nameof(CanIncrement),
                    nameof(CanDecrement),
                    nameof(IncrementHint),
                    nameof(DecrementHint),
                ],
            };

        protected override void OnTroopChange() => PlayerWarnedAboutRetraining = false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private SkillData? SkillInfo =>
            State.SkillData.TryGetValue(Skill, out var data) ? data : null;

        private TextObject CantIncrementReason =>
            SkillManager.GetIncrementSkillReason(State.Troop, Skill);

        private TextObject CantDecrementReason =>
            SkillManager.GetDecrementSkillReason(State.Troop, Skill);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public int Value => (SkillInfo?.Value ?? 0) + (SkillInfo?.Train?.PointsRemaining ?? 0);

        [DataSourceProperty]
        public string ValueColor => IsStaged ? "#ebaf2fff" : "#F4E1C4FF";

        [DataSourceProperty]
        public bool IsStaged => SkillInfo?.Train?.PointsRemaining > 0;

        [DataSourceProperty]
        public string SkillId => Skill?.StringId ?? string.Empty;

        [DataSourceProperty]
        public bool CanIncrement => CantIncrementReason == null;

        [DataSourceProperty]
        public bool CanDecrement => CantDecrementReason == null;

        /* ━━━━━━━ Tooltips ━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel IncrementHint =>
            CanIncrement ? null : Tooltip.MakeTooltip(null, CantIncrementReason.ToString());

        [DataSourceProperty]
        public BasicTooltipViewModel DecrementHint =>
            CanDecrement ? null : Tooltip.MakeTooltip(null, CantDecrementReason.ToString());

        [DataSourceProperty]
        public BasicTooltipViewModel SkillHint => Tooltip.MakeTooltip(null, Skill.Name.ToString());

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static bool PlayerWarnedAboutRetraining = false || State.IsStudioMode;

        /// <summary>
        /// Increment this skill on the selected troop.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteIncrement() => Modify(true);

        /// <summary>
        /// Decrement this skill on the selected troop.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteDecrement() => Modify(false);

        /// <summary>
        /// Perform the skill modification (increment or decrement), prompting when necessary.
        /// </summary>
        private void Modify(bool increment)
        {
            if (Config.TrainingTakesTime == false) // Only check in instant training mode
                if (
                    ContextManager.IsAllowedInContextWithPopup(
                        State.Troop,
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

                    SkillManager.ModifySkill(State.Troop, Skill, increment);
                }

                State.UpdateSkillData();
            }

            // Warn the player if decrementing a skill may require retraining
            if (
                !State.IsStudioMode // No warning in Studio Mode
                && !DoctrineAPI.IsDoctrineUnlocked<AdaptiveTraining>() // No warning if Adaptive Training is unlocked
                && !Config.RefundXpOnDecrease // No warning if refunds are enabled
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
