using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Win a battle against overwhelming odds while fielding mostly retinues.
    /// </summary>
    public sealed class Feat_Immortals_DefyTheTide : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.IM_DefyTheTide.Id;

        static bool IsMostlyRetinues;

        protected override void OnBattleStart(MMapEvent battle)
        {
            // Check if the player's party is mostly retinues at the start of the battle.
            IsMostlyRetinues = Player.Party.RetinueRatio >= 0.75f;
        }

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            if (!IsMostlyRetinues)
                return; // Not mostly retinues.

            var ally = start.PlayerSide.HealthyTroops;
            var enemy = start.EnemySide.HealthyTroops;

            if (ally <= 0 || enemy <= 0)
                return; // No troops on one side.

            if (enemy < ally * 3)
                return; // Not overwhelming odds.

            Feat.Add();
        }
    }
}
