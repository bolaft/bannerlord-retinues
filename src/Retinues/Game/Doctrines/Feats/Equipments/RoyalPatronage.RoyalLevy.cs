using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Settlements.Wrappers;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Recruit 100 custom kingdom troops.
    /// </summary>
    public sealed class Feat_RoyalPatronage_RoyalLevy : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.RP_RoyalLevy.Id;

        protected override void OnTroopRecruited(
            WHero recruiter,
            WSettlement settlement,
            WHero source,
            WCharacter troop,
            int amount
        )
        {
            if (amount <= 0)
                return; // No troops recruited.

            if (!recruiter.IsMainHero)
                return; // Not the player.

            var kingdom = Player.Kingdom;
            if (kingdom == null)
                return; // Player has no kingdom.

            if (!troop.IsFactionTroop)
                return; // Not faction troop.

            if (!troop.BelongsTo(kingdom))
                return; // Troop does not belong to player's kingdom.

            Feat.Add(amount);
        }
    }
}
