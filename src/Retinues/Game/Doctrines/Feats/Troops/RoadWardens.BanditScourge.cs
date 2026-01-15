using Retinues.Domain.Events.Models;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;

namespace Retinues.Game.Doctrines.FeatCatalog.Troops
{
    /// <summary>
    /// Clear a bandit hideout.
    /// </summary>
    public sealed class Feat_RoadWardens_BanditScourge : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.RW_BanditScourge.Id;

        protected override void OnHideoutBattleCompleted(
            BattleSideEnum winnerSide,
            MMapEvent battle,
            HideoutEventComponent hideoutEventComponent
        )
        {
            if (battle.IsLost)
                return; // Player lost the battle.

            Feat.Add();
        }
    }
}
