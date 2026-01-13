using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Game.Doctrines.Feats.Troops
{
    /// <summary>
    /// Promote 100 faction troops.
    /// </summary>
    public sealed class Feat_Captains_MeritoriousService : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_trp_meritorious_service";

        protected override void OnPlayerUpgradedTroops(
            WCharacter source,
            WCharacter target,
            int number
        )
        {
            if (!source.InCustomTree || !target.InCustomTree)
                return;

            Progress(number);
        }
    }
}
