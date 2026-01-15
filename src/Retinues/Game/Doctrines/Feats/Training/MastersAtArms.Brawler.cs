using System.Linq;
using Retinues.Domain.Events.Models;
using Retinues.Game.Missions;
using TaleWorlds.Core;

namespace Retinues.Game.Doctrines.FeatCatalog.Training
{
    /// <summary>
    /// Knock out 50 opponents in the arena.
    /// </summary>
    public sealed class Feat_MastersAtArms_Brawler : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.MA_Brawler.Id;

        protected override void OnMissionEnded(MMission mission)
        {
            if (!mission.IsArena)
                return; // Not an arena mission.

            int count = CombatBehavior
                .GetKills()
                .Count(k =>
                    k.Killer.IsPlayer // Player killer
                    && k.State == AgentState.Unconscious // Knocked out
                );

            Feat.Add(count);
        }
    }
}
