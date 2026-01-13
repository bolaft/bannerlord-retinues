using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Settlements.Wrappers;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Recruit 100 custom kingdom troops.
    /// </summary>
    public sealed class Feat_RoyalPatronage_RoyalLevy : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_royal_levy";

        protected override void OnTroopRecruited(
            WHero recruiterHero,
            WSettlement recruitmentSettlement,
            WHero recruitmentSource,
            WCharacter troop,
            int amount
        )
        {
            if (amount <= 0)
                return;

            if (!recruiterHero.IsMainHero)
                return;

            var kingdom = Player.Kingdom;
            if (kingdom == null)
                return;

            if (!troop.InCustomTree)
                return;

            if (!troop.BelongsTo(kingdom))
                return;

            Progress(amount);
        }
    }
}
