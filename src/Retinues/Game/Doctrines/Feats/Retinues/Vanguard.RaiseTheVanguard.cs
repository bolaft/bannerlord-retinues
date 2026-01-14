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
            WHero recruiter,
            WSettlement settlement,
            WHero source,
            WCharacter troop,
            int amount
        )
        {
            if (!recruiter.IsMainHero)
                return; // Not the player.

            if (!troop.IsRetinue)
                return; // Not a retinue troop.

            Progress(amount);
        }
    }
}
