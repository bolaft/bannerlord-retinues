using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Game.Doctrines.Feats.Training
{
    /// <summary>
    /// Upgrade 100 faction troops to the next tier.
    /// </summary>
    public sealed class Feat_IronDiscipline_ForgedInBattle : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.ID_ForgedInBattle.Id;

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
