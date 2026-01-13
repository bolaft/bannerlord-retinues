using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Settlements.Wrappers;

namespace Retinues.Game.Doctrines.Feats.Retinues
{
    /// <summary>
    /// Hire 100 retinues.
    /// </summary>
    public sealed class Feat_Vanguard_RaiseTheVanguard : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_ret_raise_the_vanguard";

        protected override void OnTroopRecruited(
            WHero recruiterHero,
            WSettlement recruitmentSettlement,
            WHero recruitmentSource,
            WCharacter troop,
            int amount
        )
        {
            if (!recruiterHero.IsMainHero)
                return;

            if (!troop.IsRetinue)
                return;

            Progress(amount);
        }
    }
}
