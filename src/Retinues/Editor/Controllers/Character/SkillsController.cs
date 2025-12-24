using System;
using Retinues.Helpers;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Editor.Controllers.Character
{
    public class SkillsController : EditorController
    {
        const int MaxBatch = int.MaxValue;
        const int MaxSkillLevel = 330;
        const int MinSkillLevel = 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Increase                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<SkillObject> SkillIncrease { get; } =
            Action<SkillObject>("SkillIncrease")
                .AddCondition(
                    s => State.Character.Editable.Skills.Get(s) < MaxSkillLevel,
                    L.T("skill_increase_maxed_reason", "Skill is already at maximum level.")
                )
                .ExecuteWith(s => ChangeSkill(s, +1))
                .WhenMode(
                    EditorMode.Player,
                    m =>
                        m.AddCondition(
                                s => State.Character.SkillPoints > 0,
                                L.T("skill_increase_no_points_reason", "Not enough skill points.")
                            )
                            .PostExecute(s =>
                            {
                                State.Character.SkillPoints -= 1;
                            })
                )
                .Fire(UIEvent.Skill, EventScope.Local);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Decrease                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<SkillObject> SkillDecrease { get; } =
            Action<SkillObject>("SkillDecrease")
                .AddCondition(
                    s => State.Character.Editable.Skills.Get(s) > MinSkillLevel,
                    L.T("skill_decrease_mined_reason", "Cannot decrease skill below 0.")
                )
                .WhenMode(
                    EditorMode.Player,
                    m =>
                        m.PostExecute(s =>
                        {
                            State.Character.SkillPoints += 1;
                        })
                )
                .ExecuteWith(s => ChangeSkill(s, -1))
                .Fire(UIEvent.Skill, EventScope.Local);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static void ChangeSkill(SkillObject skill, int delta)
        {
            int amount = Inputs.BatchInput(MaxBatch);
            var skills = State.Character.Editable.Skills;

            Func<SkillObject, bool> check = delta > 0 ? SkillIncrease.Allow : SkillDecrease.Allow;

            while (check(skill) && amount > 0)
            {
                skills.Modify(skill, delta);
                amount--;
            }
        }
    }
}
