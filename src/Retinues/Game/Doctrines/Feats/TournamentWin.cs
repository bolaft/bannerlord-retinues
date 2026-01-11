using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Settlements.Models;
using Retinues.Framework.Runtime;
using Retinues.UI.Services;
using TaleWorlds.Core;

namespace Retinues.Game.Doctrines.Feats
{
    /// <summary>
    /// Completes when the player wins a tournament.
    /// </summary>
    [SafeClass]
    public sealed class Feat_TournamentWin : FeatCampaignBehavior
    {
        protected override string FeatId => "tournament_win";

        protected override void OnTournamentFinished(
            WCharacter winner,
            List<WCharacter> participants,
            MTown town,
            ItemObject prize
        )
        {
            // Example: only count when the player wins.
            if (winner == null || winner.Base != Player.Hero?.Base?.CharacterObject)
                return;

            Complete(L.T("feat_src_tournament_win", "Tournament won"));
        }
    }
}
