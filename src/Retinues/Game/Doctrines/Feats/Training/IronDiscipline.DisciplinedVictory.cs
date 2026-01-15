using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.FeatCatalog.Training
{
    /// <summary>
    /// Defeat a party twice your size using only faction troops.
    /// </summary>
    public sealed class Feat_IronDiscipline_DisciplinedVictory : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.ID_DisciplinedVictory.Id;

        static bool IsAllCustom;

        protected override void OnBattleStart(MMapEvent battle)
        {
            // Check if the player's party is custom-only at the start of the battle.
            IsAllCustom = Player.Party.CustomRatio >= 1f;
        }

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            var friendly = start.PlayerSide.HealthyTroops;
            var enemy = start.EnemySide.HealthyTroops;

            if (friendly <= 0 || enemy <= 0)
                return; // No troops on one side.

            if (friendly * 2 > enemy)
                return; // Not outnumbered enough.

            if (!IsAllCustom)
                return; // Not all custom troops.

            Feat.Add();
        }
    }
}
