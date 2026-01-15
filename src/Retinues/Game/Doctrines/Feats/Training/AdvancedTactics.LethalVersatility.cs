using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// In a single battle, get a kill using five different weapon classes.
    /// </summary>
    public sealed class Feat_AdvancedTactics_LethalVersatility : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.AT_LethalVersatility.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            var classes = new HashSet<int>();

            foreach (var kill in kills)
            {
                if (!kill.Victim.IsEnemyTroop)
                    continue; // Victim is not an enemy troop.

                if (!kill.Killer.IsPlayer)
                    continue; // Killer is not a player troop.

                // Record the weapon class used.
                classes.Add(kill.WeaponClass);

                // Check if we have reached five different weapon classes.
                if (classes.Count >= 5)
                {
                    Feat.Add();
                    return;
                }
            }
        }
    }
}
