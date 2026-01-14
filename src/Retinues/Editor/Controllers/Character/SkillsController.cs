using System;
using Retinues.Configuration;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Editor.Events;
using Retinues.Editor.Services.Context;
using Retinues.Framework.Runtime;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Character
{
    public class SkillsController : BaseController
    {
        // Max batch size for skill changes depending on mode.
        private static int MaxBatch => State.Mode == EditorMode.Player ? 10 : int.MaxValue;

        // One-time warning flag. Reset on UIEvent.Page/UIEvent.Character.
        private static bool _decreaseWarningShown;

        // IMPORTANT: EventManager stores listeners as WeakReference, so we MUST keep a strong ref.
        private static readonly WarningResetListener _warningResetListener = new();

        [StaticClearAction(Order = 100, Name = "SkillsController.SkillDecreaseWarning")]
        public static void ClearSkillDecreaseWarning()
        {
            _decreaseWarningShown = false;
            EventManager.Register(_warningResetListener);
        }

        private static bool SkillLimitsActive =>
            State.Mode == EditorMode.Player
            || (State.Mode == EditorMode.Universal && Settings.EnforceSkillLimitsInUniversalMode);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Increase                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<SkillObject> SkillIncrease { get; } =
            Action<SkillObject>("SkillIncrease")
                .RequireValidEditingContext()
                .AddCondition(
                    s =>
                        State.Character.Editable.Skills.Get(s) < SkillRules.MaxSkillLevel
                        == (
                            !SkillLimitsActive
                            || State.Character.Editable.Skills.Get(s)
                                < SkillRules.GetSkillTotal(State.Character)
                        ),
                    L.T("skill_increase_maxed_reason", "Skill is already at maximum level.")
                )
                .AddCondition(
                    s =>
                        !SkillLimitsActive
                        || State.Character.IsHero
                        || (State.Character.SkillTotalUsed + 1) <= State.Character.SkillTotal,
                    L.T("skill_increase_total_reason", "Total skill limit reached.")
                )
                .WhenMode(
                    EditorMode.Player,
                    m =>
                        m.AddCondition(
                            s =>
                                !Settings.EnableSkillGainSystem
                                || State.Character.IsHero
                                || State.Character.SkillPoints > 0,
                            L.T("skill_increase_no_points_reason", "Not enough skill points.")
                        )
                )
                .ExecuteWith(s => ChangeSkill(s, +1))
                .Fire(UIEvent.Skill);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Decrease                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<SkillObject> SkillDecrease { get; } =
            Action<SkillObject>("SkillDecrease")
                .RequireValidEditingContext()
                .AddCondition(
                    s => State.Character.Editable.Skills.Get(s) > 0,
                    L.T("skill_decrease_min_reason", "Cannot decrease skill below 0.")
                )
                .AddCondition(
                    CanDecreasePastUpgradeSourceFloor,
                    L.T(
                        "skill_decrease_sources_reason",
                        "Cannot decrease below upgrade source minimum."
                    )
                )
                .ExecuteWith(DecreaseWithWarning)
                .Fire(UIEvent.Skill);

        private static bool CanDecreasePastUpgradeSourceFloor(SkillObject skill)
        {
            var c = State.Character;
            if (c == null || skill == null)
                return true;

            var current = c.Editable.Skills.Get(skill);

            var floor = SkillFloors.GetUpgradeSourceSkillFloor(c, skill);

            return current > floor;
        }

        private static void DecreaseWithWarning(SkillObject skill)
        {
            if (ShouldShowDecreaseWarning(skill) && !_decreaseWarningShown)
            {
                _decreaseWarningShown = true;

                Inquiries.Popup(
                    title: L.T("skill_decrease_warning_title", "Decrease Skill"),
                    onConfirm: () =>
                    {
                        ChangeSkill(skill, -1);
                        EventManager.Fire(UIEvent.Skill);
                    },
                    description: L.T(
                        "skill_decrease_warning_text",
                        "Decreasing a skill is instant, but retraining it will take time.\n\nContinue?"
                    )
                );

                return;
            }

            ChangeSkill(skill, -1);
        }

        private static bool ShouldShowDecreaseWarning(SkillObject skill)
        {
            if (!Settings.TrainingTakesTime)
                return false;

            if (State.Mode != EditorMode.Player)
                return false;

            if (State.Character.IsHero)
                return false;

            if (State.Character.Skills.IsStaged(skill))
                return false;

            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ChangeSkill(SkillObject skill, int delta)
        {
            int amount = Inputs.BatchInput(MaxBatch);
            var c = State.Character;
            var skills = c.Editable.Skills;

            Func<SkillObject, bool> allow = delta > 0 ? SkillIncrease.Allow : SkillDecrease.Allow;

            while (allow(skill) && amount > 0)
            {
                skills.Modify(skill, delta);

                if (!c.IsHero && State.Mode == EditorMode.Player)
                {
                    if (delta > 0)
                        c.SkillPoints = Math.Max(0, c.SkillPoints - 1);
                    else if (delta < 0)
                        c.SkillPoints += 1;
                }

                amount--;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Reset Hook VM                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private sealed class WarningResetListener : EventListenerVM
        {
            [EventListener(UIEvent.Page, UIEvent.Character)]
            private void ResetWarning()
            {
                _decreaseWarningShown = false;
            }
        }
    }
}
