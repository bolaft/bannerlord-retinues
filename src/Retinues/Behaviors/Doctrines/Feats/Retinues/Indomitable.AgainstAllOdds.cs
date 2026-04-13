using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Outnumbered at least 2 to 1, win a battle while fielding only retinues.
    /// </summary>
    public sealed class Feat_Indomitable_AgainstAllOdds : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.IN_AgainstAllOdds.Id;

        static bool IsRetinueOnly;

        protected override void OnBattleStart(MMapEvent battle)
        {
            // Check if the player's party is all retinues at the start of the battle.
            IsRetinueOnly = Player.Party.RetinueRatio >= 1f;
        }

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
                return; // Player lost the battle.

            if (!IsRetinueOnly)
                return; // Not all retinues.

            var friendly = start.PlayerSide.HealthyTroops;
            var enemy = start.EnemySide.HealthyTroops;

            if (friendly <= 0 || enemy <= 0)
                return; // No troops on one side.

            if (friendly * 2 > enemy)
                return; // Not outnumbered enough.

            Feat.Add();
        }
    }
}
