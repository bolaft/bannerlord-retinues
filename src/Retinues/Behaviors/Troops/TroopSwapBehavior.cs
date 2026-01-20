using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Framework.Behaviors;

namespace Retinues.Behaviors.Troops
{
    /// <summary>
    /// Behavior to swap militia parties in settlements when certain doctrines are acquired.
    /// </summary>
    public sealed partial class TroopSwapBehavior : BaseCampaignBehavior
    {
        /// <summary>
        /// Called when a doctrine is acquired, swaps militia parties in settlements with militia overrides.
        /// </summary>
        protected override void OnDoctrineAcquired(Doctrine doctrine)
        {
            if (doctrine.Category.Id != DoctrineCatalog.CategoryTroops.Id)
                return; // Not a troop-related doctrine.

            foreach (var party in WParty.All)
            {
                if (doctrine.Id == DoctrineCatalog.StalwartMilitia.Id && party.IsMilitia)
                    DoSwapForParty(party); // Stalwart militia affects militia parties

                if (doctrine.Id == DoctrineCatalog.ArmedPeasantry.Id && party.IsVillager)
                    DoSwapForParty(party); // Armed peasantry affects villager parties

                if (doctrine.Id == DoctrineCatalog.RoadWardens.Id && party.IsCaravan)
                    DoSwapForParty(party); // Road wardens affect caravan parties
            }
        }

        /// <summary>
        /// Called on daily tick for a settlement, swaps militia party if overrides are defined.
        /// </summary>
        protected override void OnDailyTickParty(WParty party) => DoSwapForParty(party);

        /// <summary>
        /// Performs troop swap for the given party if it is a valid militia/villager/caravan party.
        /// </summary>
        private void DoSwapForParty(WParty party)
        {
            if (party == null)
                return;

            // Stalwart militia affects militia parties
            if (party.IsMilitia && DoctrineCatalog.StalwartMilitia.IsAcquired)
                party.SwapTroops(filter: t => t.IsMilitia);

            // Road wardens affects caravan parties
            if (party.IsCaravan && DoctrineCatalog.RoadWardens.IsAcquired)
                party.SwapTroops(filter: t => t.IsCaravan);

            // Armed peasantry affects villager parties
            if (party.IsVillager && DoctrineCatalog.ArmedPeasantry.IsAcquired)
                party.SwapTroops(filter: t => t.IsVillager);
        }
    }
}
