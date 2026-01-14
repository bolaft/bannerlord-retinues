using Retinues.Domain.Characters.Wrappers;
using TaleWorlds.Core;

namespace Retinues.Domain.Characters.Services.Skills
{
    public static class SkillFloors
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Upgrade sources                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Get the highest base skill value among all upgrade source troops for the given skill.
        /// </summary>
        public static int GetUpgradeSourceSkillFloor(WCharacter character, SkillObject skill)
        {
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
                    floor = v;
            }

            return floor;
        }
    }
}
