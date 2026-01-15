using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Rescue a captive lord from an enemy party.
    /// </summary>
    public sealed class Feat_PragmaticScavengers_RescueMission : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.PR_RescueMission.Id;

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (end.IsLost)
                return; // Player lost the battle.

            foreach (var party in start.EnemySide.Parties)
            {
                foreach (var e in party.PrisonRoster.Elements)
                {
                    var hero = e.Troop?.Hero;

                    if (hero?.IsLord != true)
                        continue; // Not a lord.

                    if (hero.Clan.MapFaction.StringId != Player.Clan.MapFaction.StringId)
                        continue; // Not ally.

                    Feat.Add();
                    return; // Only count once per battle.
                }
            }
        }
    }
}
