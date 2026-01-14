using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Personally defeat an enemy lord in battle.
    /// </summary>
    public sealed class Feat_LionsShare_CutTheHead : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_cut_the_head";

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            int count = kills.Count(k =>
                k.Killer.IsPlayer // Killer is the player
                && k.Victim.IsEnemyTroop // Victim is an enemy troop
                && k.Victim.Character.Hero?.IsLord == true // Victim is a hero
            );

            SetProgress(count);
        }
    }
}
