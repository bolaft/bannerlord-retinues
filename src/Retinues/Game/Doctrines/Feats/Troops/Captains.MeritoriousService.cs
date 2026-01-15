using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Game.Doctrines.FeatCatalog.Troops
{
    /// <summary>
    /// Promote 100 faction troops.
    /// </summary>
    public sealed class Feat_Captains_MeritoriousService : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.CA_MeritoriousService.Id;

        protected override void OnPlayerUpgradedTroops(
            WCharacter source,
            WCharacter target,
            int number
        )
        {
            if (!source.IsFactionTroop || !target.IsFactionTroop)
                return; // Not faction troops.

            Feat.Add(number);
        }
    }
}
