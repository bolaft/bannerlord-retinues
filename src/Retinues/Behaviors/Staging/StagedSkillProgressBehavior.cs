using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Behaviors;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Behaviors.Staging
{
    /// <summary>
    /// Advances staged skill training progress for NPC troops and applies skill points when ready.
    /// </summary>
    public sealed class StagedSkillProgressBehavior : BaseCampaignBehavior
    {
        /// <summary>
        /// Returns true when staged training is enabled via settings.
        /// </summary>
        public override bool IsActive => Settings.TrainingTakesTime;

        /// <summary>
        /// Hourly tick handler that progresses staged skill points and finalizes them when thresholds are met.
        /// </summary>
        protected override void OnHourlyTick()
        {
            if (!Settings.TrainingProgressesWhileTravelling && Player.CurrentSettlement == null)
                return;

            float pointsPerDay = Settings.SkillProgressPerDay;
            if (pointsPerDay <= 0.01f)
                pointsPerDay = 0.01f;

            float progressDelta = pointsPerDay / 24f;

            bool changed = false;

            foreach (var wc in WCharacter.All)
            {
                if (wc == null || wc.IsHero)
                    continue;

                if (!wc.HasAnyStagedSkillPoints())
                    continue;

                wc.SkillStagingProgress = MathF.Max(0f, wc.SkillStagingProgress + progressDelta);

                // Allow faster-than-1.0 multipliers to convert multiple points per hour.
                int safety = 0;
                while (
                    wc.SkillStagingProgress >= 1f && wc.HasAnyStagedSkillPoints() && safety < 128
                )
                {
                    if (!wc.TryApplyOneStagedSkillPointRandom(out var skill, out var newValue))
                        break;

                    wc.SkillStagingProgress -= 1f;
                    changed = true;
                    safety++;

                    // Message: troop name, skill name, new skill value
                    var troopName = wc.Name?.ToString() ?? wc.StringId;
                    var skillName = skill?.Name?.ToString() ?? skill?.StringId ?? "unknown";
                    Notifications.Message(
                        L.T("staged_skill_unlock", "{TROOP} improved {SKILL} to {VALUE}.")
                            .SetTextVariable("TROOP", troopName)
                            .SetTextVariable("SKILL", skillName)
                            .SetTextVariable("VALUE", newValue)
                    );
                }
                if (!wc.HasAnyStagedSkillPoints())
                    wc.SkillStagingProgress = 0f;
            }

            if (changed)
                Log.Debug("SkillStagingBehavior: applied staged skill points.");
        }
    }
}
