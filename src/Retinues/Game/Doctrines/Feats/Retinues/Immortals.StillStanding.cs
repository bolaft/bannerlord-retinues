using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;
using TaleWorlds.Core;

namespace Retinues.Game.Doctrines.FeatCatalog.Retinues
{
    /// <summary>
    /// Have 20 retinues survive being struck down in battle.
    /// </summary>
    public sealed class Feat_Immortals_StillStanding : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.IM_StillStanding.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            int count = kills.Count(k =>
                k.State == AgentState.Unconscious // Victim is still alive
                && k.Victim.IsPlayerTroop // Victim is a player troop
                && k.Victim.Character.IsRetinue // Victim is a retinue troop
            );

            Feat.Add(count);
        }
    }
}
