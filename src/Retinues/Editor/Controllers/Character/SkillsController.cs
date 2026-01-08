using System;
using Retinues.Configuration;
using Retinues.Editor.Events;
using Retinues.Editor.Services.Context;
using Retinues.Framework.Runtime;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Character
{
    public class SkillsController : BaseController
    {
        const int MaxBatch = int.MaxValue;

        // One-time warning flag. Reset on UIEvent.Page/UIEvent.Character.
        private static bool _decreaseWarningShown;

        // IMPORTANT: EventManager stores listeners as WeakReference, so we MUST keep a strong ref.
        private static readonly WarningResetListener _warningResetListener = new();

        // If EventManager.ClearAll runs (new session/load), listeners are cleared.
        // Re-register our static listener after clears so resets keep working.
        [StaticClearAction(Order = 100, Name = "SkillsController.SkillDecreaseWarning")]
        public static void ClearSkillDecreaseWarning()
        {
            _decreaseWarningShown = false;

            // Re-register after EventManager.ClearAll cleared the listener list.
            // Safe if it runs even when not cleared (it only adds one weak ref).
            EventManager.Register(_warningResetListener);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Increase                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<SkillObject> SkillIncrease { get; } =
            Action<SkillObject>("SkillIncrease")
                .RequireValidEditingContext()
                .AddCondition(
                    s => State.Character.Editable.Skills.Get(s) < State.Character.SkillCapForTier,
                    L.T("skill_increase_maxed_reason", "Skill is already at maximum level.")
                )
                .AddCondition(
                    s =>
                        State.Character.IsHero
                        || (State.Character.SkillTotalUsed + 1)
                            <= State.Character.SkillTotalMaxForTier,
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
                    s => CanDecreasePastUpgradeSourceFloor(s),
                    L.T(
                        "skill_decrease_sources_reason",
                        "Cannot decrease below upgrade source minimum."
                    )
                )
                .ExecuteWith(DecreaseWithWarning)
                .Fire(UIEvent.Skill);

        /// <summary>
        /// Check if the skill can be decreased past the minimum floor set by upgrade sources.
        /// </summary>
        private static bool CanDecreasePastUpgradeSourceFloor(SkillObject skill)
        {
            var c = State.Character;
            if (c == null || skill == null)
                return true;

            // Current editable value (real or staged depending on mode/settings).
            var current = c.Editable.Skills.Get(skill);

            var floor = GetUpgradeSourceSkillFloor(c, skill);

            // If floor is 0, this behaves like vanilla (only the >0 condition matters).
            // If floor > 0, do not allow decreasing to <= floor.
            return current > floor;
        }

        /// <summary>
        /// Get the highest skill value among all upgrade source troops for the given skill.
        /// </summary>
        private static int GetUpgradeSourceSkillFloor(
            Domain.Characters.Wrappers.WCharacter character,
            SkillObject skill
        )
        {
            var sources = character.UpgradeSources;
            if (sources == null || sources.Count == 0)
                return 0;

            var floor = 0;
            for (int i = 0; i < sources.Count; i++)
            {
                var src = sources[i];
                if (src == null)
                    continue;

                // Use the source troop's current (non-edited) skill value.
                var v = src.Skills.Get(skill);
                if (v > floor)
                    floor = v;
            }

            return floor;
        }

        /// <summary>
        /// Decrease skill with warning popup if needed.
        /// </summary>
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

                        // EditorAction will fire UIEvent.Skill even if we showed a popup and returned.
                        // State changes only on confirm, an explicit refresh is needed here.
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

        /// <summary>
        /// Determine if we should show the decrease warning for the given skill.
        /// </summary>
        private static bool ShouldShowDecreaseWarning(SkillObject skill)
        {
            // Only relevant when training takes time (staging system enabled).
            if (!Settings.TrainingTakesTime)
                return false;

            // Staging is Player mode only.
            if (State.Mode != EditorMode.Player)
                return false;

            // Heroes don't stage/retrain over time in your system.
            if (State.Character.IsHero)
                return false;

            // Only show if the skill is not currently staged.
            if (State.Character.Skills.IsStaged(skill))
                return false;

            return true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Change the skill by the given delta, handling batch input and skill points.
        /// </summary>
        private static void ChangeSkill(SkillObject skill, int delta)
        {
            int amount = Inputs.BatchInput(MaxBatch);
            var c = State.Character;
            var skills = c.Editable.Skills;

            Func<SkillObject, bool> allow = delta > 0 ? SkillIncrease.Allow : SkillDecrease.Allow;

            while (allow(skill) && amount > 0)
            {
                skills.Modify(skill, delta);

                // Spend/refund skill points only in Player mode, and only for non-heroes.
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
        //                     Reset hook VM                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Listener to reset the decrease warning flag on page/character changes.
        /// </summary>
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
