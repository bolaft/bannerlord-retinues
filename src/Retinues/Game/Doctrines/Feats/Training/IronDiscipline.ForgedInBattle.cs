using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Game.Doctrines.FeatCatalog.Training
{
    /// <summary>
    /// Upgrade 100 faction troops to the next tier.
    /// </summary>
    public sealed class Feat_IronDiscipline_ForgedInBattle : FeatCampaignBehavior
    {
        protected override string FeatId => Catalogs.DoctrineCatalog.ID_ForgedInBattle.Id;

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
