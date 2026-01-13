using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Field a unit wearing equipment worth over 100 000 denars.
    /// </summary>
    public sealed class Feat_HonorGuard_GoldenLegion : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_golden_legion";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            foreach (var e in Player.Party.MemberRoster.Elements)
            {
                if (e.Number <= 0)
                    continue;

                var troop = e.Troop;
                if (!troop.InCustomTree)
                    continue;

                foreach (var eq in troop.Equipments)
                {
                    if (!IsValidForBattle(eq, battle))
                        continue;

                    int value = 0;

                    foreach (var item in eq.Items)
                    {
                        if (item == null)
                            continue;

                        value += item.Value;
                    }

                    if (value <= 100000)
                        continue;

                    Progress(1);
                    return;
                }
            }
        }
    }
}
