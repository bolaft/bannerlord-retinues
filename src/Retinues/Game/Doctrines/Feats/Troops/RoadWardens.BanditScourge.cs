using Retinues.Domain.Events.Models;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Clear a bandit hideout.
    /// </summary>
    public sealed class Feat_RoadWardens_BanditScourge : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_bandit_scourge";

        protected override void OnHideoutBattleCompleted(
            BattleSideEnum winnerSide,
            MMapEvent battle,
            HideoutEventComponent hideoutEventComponent
        )
        {
            if (battle.IsLost)
                return;

            Progress(1);
        }
    }
}
