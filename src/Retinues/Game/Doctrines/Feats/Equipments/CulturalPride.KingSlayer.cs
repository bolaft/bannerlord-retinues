using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Defeat a ruler of a different culture in battle.
    /// </summary>
    public sealed class Feat_CulturalPride_KingSlayer : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_kingslayer";

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            foreach (var kill in kills)
            {
                var hero = kill.Victim.Character.Hero;
                if (
                    hero != null // Victim is a hero
                    && hero.IsFactionLeader // Victim is a ruler
                    && hero.Culture != Player.Culture // Different culture
                )
                {
                    Progress();
                    return;
                }
            }
        }
    }
}
