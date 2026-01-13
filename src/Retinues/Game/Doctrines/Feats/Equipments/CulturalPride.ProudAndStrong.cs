using System.Collections.Generic;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Get 100 kills in battle with troops wearing no foreign gear.
    /// </summary>
    public sealed class Feat_CulturalPride_ProudAndStrong : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_proud_and_strong";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (battle.IsLost)
                return;

            int count = 0;

            var kf = Filter(
                killers: a => a.IsPlayerTroop && a.IsCustom,
                victims: v => v.IsEnemyTroop
            );

            foreach (var kill in kf.Filter(kills))
            {
                var culture = kill.Killer.Culture;
                bool hasForeignGear = false;

                foreach (var item in kill.KillerEquipment.Items)
                {
                    if (culture != item.Culture)
                    {
                        hasForeignGear = true;
                        break;
                    }
                }

                if (!hasForeignGear)
                    count++;
            }

            Progress(count);
        }
    }
}
