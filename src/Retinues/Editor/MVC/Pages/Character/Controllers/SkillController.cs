using System;
using Retinues.Behaviors.Experience;
using Retinues.Domain.Characters.Services.Skills;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Editor.MVC.Shared.Controllers.Helpers;
using Retinues.Framework.Runtime;
using Retinues.Interface.Services;
using Retinues.Settings;
using TaleWorlds.Core;

namespace Retinues.Editor.MVC.Pages.Character.Controllers
{
    /// <summary>
    /// Controller for skill editing, enforcing limits and handling UI interactions.
    /// </summary>
    public class SkillController : BaseController
    {
        // Max batch size for skill changes depending on mode.
        private static int MaxBatch => State.Mode == EditorMode.Player ? 10 : int.MaxValue;

        // One-time warning flag. Reset on UIEvent.Page/UIEvent.Character.
        private static bool _decreaseWarningShown;

        // IMPORTANT: EventManager stores listeners as WeakReference, so we MUST keep a strong ref.
        private static readonly WarningResetListener _warningResetListener = new();

        private static bool SkillLimitsActive =>
            State.Mode == EditorMode.Player
            || (
                State.Mode == EditorMode.Universal
                && Configuration.EnforceSkillLimitsInUniversalMode
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Increase                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Increases the specified skill for the current character, respecting limits and costs.
        /// </summary>
        public static ControllerAction<SkillObject> SkillIncrease { get; } =
            Action<SkillObject>("SkillIncrease")
                .RequireValidEditingContext()
                .AddCondition(
                    s =>
                        State.Character.Skills.Get(s) < SkillRules.MaxSkillLevel
                        == (
                            !SkillLimitsActive
                            || State.Character.Skills.Get(s)
                                < SkillRules.GetSkillCap(State.Character)
                        ),
                    L.T("skill_increase_maxed_reason", "Skill cap reached")
                )
                .AddCondition(
                    s =>
                        !SkillLimitsActive
                        || State.Character.IsHero
                        || (State.Character.SkillTotalUsed + 1) <= State.Character.SkillTotal,
                    L.T("skill_increase_total_reason", "Total skill limit reached")
                )
                .WhenMode(
                    EditorMode.Player,
                    m =>
                        m.AddCondition(
                            s =>
                                !Configuration.SkillPointsMustBeEarned
                                || State.Character.IsHero
                                || (
                                    Configuration.SharedSkillPointsPool
                                        ? SharedSkillPoolBehavior.SharedSkillPoints > 0
                                        : State.Character.SkillPoints > 0
                                ),
                            L.T("skill_increase_no_points_reason", "Not enough skill points")
                        )
                )
                .ExecuteWith(s => ChangeSkill(s, +1))
                .Fire(UIEvent.Skill);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Decrease                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // The character that sets the current decrease floor (if any).
        static WCharacter _sourceFloorCharacter;

        /// <summary>
        /// Decreases the specified skill for the current character, with upgrade-source safeguards.
        /// </summary>
        public static ControllerAction<SkillObject> SkillDecrease { get; } =
            Action<SkillObject>("SkillDecrease")
                .RequireValidEditingContext()
                .AddCondition(s => State.Character.Skills.Get(s) > 0)
                .AddCondition(
                    CanDecreasePastUpgradeSourceFloor,
                    () =>
                        _sourceFloorCharacter != null
                            ? L.T(
                                    "skill_decrease_sources_reason",
                                    "Cannot decrease below {SOURCE}'s skill level"
                                )
                                .SetTextVariable("SOURCE", _sourceFloorCharacter.Name)
                            : L.T(
                                "skill_decrease_sources_reason_generic",
                                "Cannot decrease below upgrade sources' skill levels"
                            )
                )
                .ExecuteWith(DecreaseWithWarning)
                .Fire(UIEvent.Skill);

        private static bool CanDecreasePastUpgradeSourceFloor(SkillObject skill)
        {
            _sourceFloorCharacter = null;

            var c = State.Character;
            if (c == null || skill == null)
                return true;

            var current = c.Skills.Get(skill);

            var floor = GetUpgradeSourceSkillFloor(c, skill, out _sourceFloorCharacter);

            return current > floor;
        }

        /// <summary>
        /// Get the highest base skill value among all upgrade source troops for the given skill.
        /// </summary>
        public static int GetUpgradeSourceSkillFloor(
            WCharacter character,
            SkillObject skill,
            out WCharacter source
        )
        {
            source = null;

            if (character == null || skill == null)
                return 0;

            var sources = character.UpgradeSources;
            if (sources == null || sources.Count == 0)
                return 0;

            var floor = 0;

            for (int i = 0; i < sources.Count; i++)
            {
                var src = sources[i];
                if (src == null)
                    continue;

                // Source floor should reflect the real/base minimum.
                var v = src.Skills.GetBase(skill);

                if (v > floor)
                {
                    source = src;
                    floor = v;
                }
            }

            return floor;
        }

        /// <summary>
        /// Clears the one-time skill-decrease warning flag and registers the reset listener.
        /// </summary>
        [StaticClearAction(Order = 100, Name = "SkillsController.SkillDecreaseWarning")]
        public static void ClearSkillDecreaseWarning()
        {
            _decreaseWarningShown = false;
            EventManager.Register(_warningResetListener);
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

        /// <summary>
        /// Returns whether a decrease warning should be shown for this skill change.
        /// </summary>
        private static bool ShouldShowDecreaseWarning(SkillObject skill)
        {
            if (!Configuration.TrainingTakesTime)
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
        //                          Reset                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Resets every skill on the current troop down to its minimum — its upgrade-source floor,
        /// which is 0 for troops without sources — refunding one skill point per level removed,
        /// exactly as repeatedly clicking the decrease chevron would.
        /// </summary>
        public static ControllerAction<WCharacter> ResetSkills { get; } =
            Action<WCharacter>("ResetSkills")
                .RequireValidEditingContext()
                .DefaultTooltip(L.T("reset_skills_tooltip", "Reset all skills"))
                .AddCondition(
                    c => c != null && !c.IsHero,
                    L.T("reset_skills_troops_only", "Only available for troops")
                )
                .AddCondition(
                    c => c != null && c.SkillTotalUsed > 0,
                    L.T("reset_skills_nothing", "No skills to reset")
                )
                .ExecuteWith(ResetSkillsImpl)
                .Fire(UIEvent.Skill);

        private static void ResetSkillsImpl(WCharacter c)
        {
            if (c == null || c.IsHero)
                return;

            Inquiries.Popup(
                title: L.T("reset_skills_confirm_title", "Reset Skills"),
                description: L.T(
                        "reset_skills_confirm_body",
                        "Reset all of {NAME}'s skills? Every spent skill point will be refunded."
                    )
                    .SetTextVariable("NAME", c.Name),
                confirmText: L.T("reset_skills_confirm_yes", "Reset"),
                cancelText: L.T("reset_skills_confirm_no", "Cancel"),
                onConfirm: () =>
                {
                    ResetAllSkills(c);
                    EventManager.Fire(UIEvent.Skill);
                }
            );
        }

        /// <summary>
        /// Decreases every skill to its floor, mirroring the per-level point refund of a manual
        /// decrease (player mode only; the Universal Editor has no point economy).
        /// </summary>
        private static void ResetAllSkills(WCharacter c)
        {
            if (c == null || c.IsHero)
                return;

            var skills = c.Skills;
            bool refundPoints = State.Mode == EditorMode.Player;

            foreach (var skill in SkillCatalog.GetSkills(c))
            {
                if (skill == null)
                    continue;

                int floor = Math.Max(0, GetUpgradeSourceSkillFloor(c, skill, out _));

                // Guard against any pathological non-decreasing case.
                int guard = 10000;
                while (skills.Get(skill) > floor && guard-- > 0)
                {
                    skills.Modify(skill, -1);

                    if (refundPoints)
                    {
                        if (Configuration.SharedSkillPointsPool)
                            SharedSkillPoolBehavior.SharedSkillPoints += 1;
                        else
                            c.SkillPoints += 1;
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Change the given skill by delta, honoring batch input and point refunds/costs.
        /// </summary>
        private static void ChangeSkill(SkillObject skill, int delta)
        {
            int amount = InputHelper.BatchInput(MaxBatch);
            var c = State.Character;
            var skills = c.Skills;

            Func<SkillObject, bool> allow = delta > 0 ? SkillIncrease.Allow : SkillDecrease.Allow;

            while (allow(skill) && amount > 0)
            {
                skills.Modify(skill, delta);

                if (!c.IsHero && State.Mode == EditorMode.Player)
                {
                    if (Configuration.SharedSkillPointsPool)
                    {
                        if (delta > 0)
                            SharedSkillPoolBehavior.SharedSkillPoints = Math.Max(
                                0,
                                SharedSkillPoolBehavior.SharedSkillPoints - 1
                            );
                        else if (delta < 0)
                            SharedSkillPoolBehavior.SharedSkillPoints += 1;
                    }
                    else
                    {
                        if (delta > 0)
                            c.SkillPoints = Math.Max(0, c.SkillPoints - 1);
                        else if (delta < 0)
                            c.SkillPoints += 1;
                    }
                }

                amount--;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Reset Hook VM                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Listener to reset the decrease-warning flag on page/character changes.
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
