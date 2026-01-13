using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Settlements.Wrappers;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Defeat a ruler of a different culture in battle.
    /// </summary>
    public sealed class Feat_CulturalPride_HometownTournament : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_hometown_tournament";

        protected override void OnTournamentFinished(
            WCharacter winner,
            List<WCharacter> participants,
            WSettlement settlement,
            WItem prize
        )
        {
            if (!winner.IsPlayer)
                return;

            if (settlement.Culture != Player.Culture)
                return;

            Progress(1);
        }
    }
}
