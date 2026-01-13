using Retinues.Domain.Events.Models;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Field a unit wearing equipment weighing over 60 kg.
    /// </summary>
    public sealed class Feat_Ironclad_HeavyKit : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_heavy_kit";

        protected override void OnBattleStart(MMapEvent battle)
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

                    float weight = 0;

                    foreach (var item in eq.Items)
                    {
                        if (item == null)
                            continue;

                        weight += item.Weight;
                    }

                    if (weight <= 60)
                        continue;

                    Progress(1);
                    return;
                }
            }
        }
    }
}
