using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Upgrade 100 elite faction troops to the next tier.
    /// </summary>
    public sealed class Feat_MastersAtArms_DistinguishedService : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.MA_DistinguishedService.Id;

        protected override void OnPlayerUpgradedTroops(
            WCharacter source,
            WCharacter target,
            int number
        )
        {
            if (!source.IsFactionTroop || !target.IsFactionTroop)
                return; // Not faction troops.

            if (!source.IsElite || !target.IsElite)
                return; // Not elite troops.

            Feat.Add(number);
        }
    }
}
