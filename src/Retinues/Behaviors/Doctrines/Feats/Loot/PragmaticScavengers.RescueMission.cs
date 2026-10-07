using System.Collections.Generic;
using Retinues.Behaviors.Missions;
using Retinues.Domain;
using Retinues.Domain.Events.Models;

namespace Retinues.Behaviors.Doctrines.Feats.Loot
{
    /// <summary>
    /// Rescue a captive lord from an enemy party.
    /// </summary>
    public sealed class Feat_PragmaticScavengers_RescueMission : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.PR_RescueMission.Id;

        static bool HasCaptiveAlliedLord;

        protected override void OnBattleStart(MMapEvent battle)
        {
            HasCaptiveAlliedLord = false;

            // (as of 1.4?) defeated prisoners are removed from enemy rosters before OnMapEventEnded and listeners can no longer detect the lord being rescued.
            // captive allied lord check can be done in battle start instead
            foreach (var party in battle.EnemySide.Parties)
            {
                foreach (var e in party.PrisonRoster.Elements)
                {
                    var hero = e.Troop?.Hero;

                    if (hero?.IsLord != true)
                        continue; // Not a lord.

                    if (hero.Clan.MapFaction.StringId != Player.Clan.MapFaction.StringId)
                        continue; // Not ally.

                    HasCaptiveAlliedLord = true;
                    return;
                }
            }
        }

        protected override void OnBattleOver(
            IReadOnlyList<CombatBehavior.Kill> kills,
            MMapEvent.Snapshot start,
            MMapEvent end
        )
        {
            if (!end.IsWon)
                return; // Player lost the battle.

            if (!HasCaptiveAlliedLord)
                return; // No allied lord was held by the enemy at battle start.

            Feat.Add();
        }
    }
}
