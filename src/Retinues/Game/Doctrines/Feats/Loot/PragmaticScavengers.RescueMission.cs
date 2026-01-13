using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Loot
{
    /// <summary>
    /// Rescue a captive lord from an enemy party.
    /// </summary>
    public sealed class Feat_PragmaticScavengers_RescueMission : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_sp_rescue_mission";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            foreach (var party in battle.EnemySideParties)
            {
                if (party == null)
                    continue;

                foreach (var e in party.PrisonRoster.Elements)
                {
                    var hero = e.Troop?.Hero;

                    if (hero?.IsLord != true)
                        continue;

                    if (hero.Clan.MapFaction.StringId != Player.Clan.MapFaction.StringId)
                        continue;

                    Progress(1);
                    return;
                }
            }
        }
    }
}
