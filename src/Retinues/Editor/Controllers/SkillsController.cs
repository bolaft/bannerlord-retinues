using System;
using Retinues.Engine;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers
{
    public class SkillsController : BaseController
    {
        const int MaxBatch = 10;
        const int MaxSkillLevel = 330;
        const int MinSkillLevel = 0;

        /// <summary>
        /// Increase the skill of the selected character.
        /// </summary>
        public static void IncreaseSkill(SkillObject skill) => ChangeSkill(skill, +1);

        /// <summary>
        /// Decrease the skill of the selected character.
        /// </summary>
        public static void DecreaseSkill(SkillObject skill) => ChangeSkill(skill, -1);

        /// <summary>
        /// Change the skill of the selected character iteratively until some condition is met.
        /// </summary>
        private static void ChangeSkill(SkillObject skill, int delta)
        {
            int amount = Inputs.BatchInput(MaxBatch);
            var skills = State.Character.Skills;

            Func<SkillObject, bool> check =
                delta > 0
                    ? s => CanIncreaseSkill(s, out var r)
                    : s => CanDecreaseSkill(s, out var r);

            while (check(skill) && amount > 0)
            {
                skills.Modify(skill, delta);
                amount--;
            }

            EventManager.Fire(UIEvent.Skill, EventScope.Local);
        }

        /// <summary>
        /// Check if the skill can be increased.
        /// </summary>
        public static bool CanIncreaseSkill(SkillObject skill, out TextObject reason) =>
            Check(
                [
                    (
                        () => State.Character.Skills.Get(skill) < MaxSkillLevel,
                        L.T("skill_increase_maxed_reason", "Skill is already at maximum level.")
                    ),
                ],
                out reason
            );

        /// <summary>
        /// Check if the skill can be decreased.
        /// </summary>
        public static bool CanDecreaseSkill(SkillObject skill, out TextObject reason) =>
            Check(
                [
                    (
                        () => State.Character.Skills.Get(skill) > MinSkillLevel,
                        L.T("skill_decrease_mined_reason", "Cannot decrease skill below 0.")
                    ),
                ],
                out reason
            );
    }
}
