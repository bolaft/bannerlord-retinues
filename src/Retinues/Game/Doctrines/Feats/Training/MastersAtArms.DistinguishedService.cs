using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Upgrade 100 elite faction troops to the next tier.
    /// </summary>
    public sealed class Feat_MastersAtArms_DistinguishedService : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_tr_distinguished_service";

        protected override void OnPlayerUpgradedTroops(
            WCharacter source,
            WCharacter target,
            int number
        )
        {
            if (!source.InCustomTree || !target.InCustomTree)
                return;

            if (!source.IsElite || !target.IsElite)
                return;

            Progress(number);
        }
    }
}
