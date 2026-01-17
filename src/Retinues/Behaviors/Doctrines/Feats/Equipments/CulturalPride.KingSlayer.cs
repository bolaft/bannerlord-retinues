using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Defeat a ruler of a different culture in battle.
    /// </summary>
    public sealed class Feat_CulturalPride_KingSlayer : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.CP_KingSlayer.Id;

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
                    Feat.Add();
                    return;
                }
            }
        }
    }
}
