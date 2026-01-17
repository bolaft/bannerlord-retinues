using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Win a defensive battle with a retinue-only party.
    /// </summary>
    public sealed class Feat_Indomitable_HoldTheLine : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.IN_HoldTheLine.Id;

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

            if (!start.DefenderSide.IsPlayerSide)
                return; // Not a defensive battle.

            if (!IsAllRetinue)
                return; // Not all retinues.

            Feat.Add();
        }
    }
}
