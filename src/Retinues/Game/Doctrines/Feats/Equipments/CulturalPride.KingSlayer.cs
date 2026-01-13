using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Events.Models;
using static Retinues.Domain.Events.Models.MMission;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Win a tournament in a town of your clan's culture.
    /// </summary>
    public sealed class Feat_CulturalPride_KingSlayer : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_king_slayer";

        protected override void OnBattleOver(IReadOnlyList<Kill> kills, MMapEvent battle)
        {
            if (!battle.IsWon)
                return;

            var kf = Filter(
                killers: a => a.Character.IsPlayer,
                victims: v =>
                    v.IsEnemyTroop
                    && v.Character.IsHero
                    && v.Character.Hero.IsFactionLeader
                    && v.Character.Culture != Player.Culture
            );

            var victim = kf.Filter(kills).FirstOrDefault().Victim?.Hero;
            if (victim == null)
                return;

            Progress(1);
            return;
        }
    }
}
