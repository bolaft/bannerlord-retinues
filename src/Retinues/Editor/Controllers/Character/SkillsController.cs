using System;
using Retinues.Configuration;
using Retinues.Editor.Events;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Character
{
    public class SkillsController : BaseController
    {
        const int MaxBatch = int.MaxValue;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Increase                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<SkillObject> SkillIncrease { get; } =
            Action<SkillObject>("SkillIncrease")
                // Skill cap
                .AddCondition(
                    s => State.Character.Editable.Skills.Get(s) < State.Character.SkillCapForTier,
                    L.T("skill_increase_maxed_reason", "Skill is already at maximum level.")
                )
                // Skill total
                .AddCondition(
                    s =>
                        State.Character.IsHero
                        || (State.Character.SkillTotalUsed + 1)
                            <= State.Character.SkillTotalMaxForTier,
                    L.T("skill_increase_total_reason", "Total skill limit reached.")
                )
                // Spend points only in Player mode, and only for non-heroes.
                .WhenMode(
                    EditorMode.Player,
                    m =>
                        m.AddCondition(
                            s =>
                                !Settings.EnableSkillPointsSystem
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
                .AddCondition(
                    s => State.Character.Editable.Skills.Get(s) > 0,
                    L.T("skill_decrease_min_reason", "Cannot decrease skill below 0.")
                )
                .ExecuteWith(s => ChangeSkill(s, -1))
                .Fire(UIEvent.Skill);

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
    }
}
