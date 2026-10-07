using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Domain.Settlements.Wrappers;

namespace Retinues.Behaviors.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Have a companion of the same culture as your kingdom govern a kingdom fief for 30 days.
    /// </summary>
    public sealed class Feat_RoyalPatronage_RoyalStewardship : BaseFeatBehavior
    {
        protected override string FeatId => Catalogs.FeatCatalog.RP_RoyalStewardship.Id;

        protected override void OnDailyTick()
        {
            var kingdom = Player.Kingdom;
            if (kingdom == null)
                return; // Player has no kingdom.

            if (HasMatchingGovernor(Player.Clan?.Settlements, kingdom.Culture))
                Feat.Add();
        }

        internal static bool HasMatchingGovernor(
            IEnumerable<WSettlement> settlements,
            WCulture kingdomCulture
        )
        {
            if (settlements == null || kingdomCulture == null)
                return false;

            foreach (var s in settlements)
            {
                // Clan settlements include bound villages, which have no Town component.
                var governor = s?.Town?.Governor;
                if (governor == null)
                    continue; // No governor.

                if (!governor.IsCompanion)
                    continue; // Not a companion.

                if (governor.Culture != kingdomCulture)
                    continue;

                return true;
            }

            return false;
        }
    }
}
