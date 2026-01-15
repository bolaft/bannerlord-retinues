using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Win a battle of 100 or more combatants using only your retinues.
    /// </summary>
    public sealed class Feat_Vanguard_ShockAssault : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.VA_ShockAssault.Id;

        static bool IsAllRetinue;

        protected override void OnBattleStart(MMapEvent battle)
        {
            // Check if the player's party is retinue-only at the start of the battle.
            IsAllRetinue = Player.Party.RetinueRatio >= 1f;
        }

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            if ((start.PlayerSide.HealthyTroops + start.EnemySide.HealthyTroops) < 100)
                return; // Not enough combatants.

            if (!IsAllRetinue)
                return; // Not all retinues.

            Feat.Add();
        }
    }
}
