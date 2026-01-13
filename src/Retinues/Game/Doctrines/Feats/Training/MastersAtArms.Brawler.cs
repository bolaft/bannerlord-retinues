using Retinues.Domain.Events.Models;
using TaleWorlds.Core;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Knock out 50 opponents in the arena.
    /// </summary>
    public sealed class Feat_MastersAtArms_Brawler : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_brawler";

        protected override void OnMissionEnded(MMission mission)
        {
            if (!mission.IsArena)
                return;

            var kf = Filter(killers: a => a.IsPlayerCharacter);

            foreach (var kill in kf.Filter(mission.Kills))
            {
                if (kill.State == AgentState.Unconscious)
                    Progress(1);
            }
        }
    }
}
