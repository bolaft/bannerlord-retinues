using Retinues.Domain.Characters.Wrappers;

namespace Retinues.Game.Doctrines.Feats.Equipments
{
    /// <summary>
    /// Have a companion of the same culture as your kingdom govern a kingdom fief for 30 days.
    /// </summary>
    public sealed class Feat_RoyalPatronage_RoyalStewardship : FeatCampaignBehavior
    {
        protected override string FeatId => "feat_eq_royal_stewardship";

        protected override void OnDailyTick()
        {
            var kingdom = Player.Kingdom;
            if (kingdom == null)
                return; // Player has no kingdom.

            // Find a companion governor of the same culture as the kingdom.
            WHero match = null;

            foreach (var s in Player.Clan.Settlements)
            {
                var governor = s.Town.Governor;
                if (governor == null)
                    continue; // No governor.

                if (!governor.IsCompanion)
                    continue; // Not a companion.

                if (governor.Culture != Player.Kingdom.Culture)
                    continue;

                // Found a match.
                match = governor;
                break;
            }

            if (match == null)
                return; // No matching governor.

            Progress();
        }
    }
}
