using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Domain.Settlements.Wrappers;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Win a tournament in a town of your clan's culture.
    /// </summary>
    public sealed class Feat_CulturalPride_HometownTournament : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.CP_HometownTournament.Id;

        protected override void OnTournamentFinished(
            WCharacter winner,
            List<WCharacter> participants,
            WSettlement settlement,
            WItem prize
        )
        {
            if (!winner.IsPlayer)
                return; // Not the player.

            if (settlement.Culture != Player.Culture)
                return; // Not the player's culture.

            Feat.Add();
        }
    }
}
