using Retinues.Domain;
using Retinues.Framework.Behaviors;

namespace Retinues.Behaviors.Troops
{
    /// <summary>
    /// Unlocks and assigns custom troops for the player clan and kingdom by cloning culture roots/trees.
    /// </summary>
    public sealed partial class TroopUnlockerBehavior : BaseCampaignBehavior<TroopUnlockerBehavior>
    {
        /// <summary>
        /// Public API to attempt unlocking faction troops immediately.
        /// </summary>
        public static void TryUnlockNow(bool fromBootstrap = false)
        {
            if (!TryGetInstance(out var behavior) || behavior == null)
                return;

            behavior.TryUnlockFromCurrentState(fromBootstrap);
        }

        /// <summary>
        /// Attempts to unlock clan and kingdom troops based on current player state.
        /// </summary>
        private void TryUnlockFromCurrentState(bool fromBootstrap)
        {
            TryUnlockClanTroops(Player.Clan, fromBootstrap);
            TryUnlockKingdomTroops(Player.Kingdom, fromBootstrap);

            TryUnlockDoctrineTroopsForPlayerFactions(fromBootstrap);
        }
    }
}
