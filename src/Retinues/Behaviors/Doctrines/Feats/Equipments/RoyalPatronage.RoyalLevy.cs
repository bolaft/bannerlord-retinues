using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Behaviors.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Recruit 100 custom kingdom troops.
    /// </summary>
    public sealed class Feat_RoyalPatronage_RoyalLevy : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.RP_RoyalLevy.Id;

        protected override void OnPlayerRecruitedTroops(WCharacter troop, int amount)
        {
            if (amount <= 0)
                return; // No troops recruited.

            var kingdom = Player.Kingdom;
            if (kingdom == null)
                return; // Player has no kingdom.

            if (troop == null || !troop.IsFactionTroop)
                return; // Not faction troop.

            if (!troop.BelongsTo(kingdom))
                return; // Troop does not belong to player's kingdom.

            Feat.Add(amount);
        }
    }
}
