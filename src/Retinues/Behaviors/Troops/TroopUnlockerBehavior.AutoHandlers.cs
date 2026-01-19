using System;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace Retinues.Behaviors.Troops
{
    public sealed partial class TroopUnlockerBehavior
    {
        /// <summary>
        /// Called after character creation completes; triggers bootstrap unlock checks.
        /// </summary>
        protected override void OnCharacterCreationIsOver() =>
            TryUnlockFromCurrentState(fromBootstrap: true);

        /// <summary>
        /// Called when game load is finished; triggers bootstrap unlock checks.
        /// </summary>
        protected override void OnGameLoadFinished() =>
            TryUnlockFromCurrentState(fromBootstrap: true);

        /// <summary>
        /// Handles settlement owner changes to trigger clan troop unlocks when player gains a fief.
        /// </summary>
        protected override void OnSettlementOwnerChanged(
            WSettlement settlement,
            bool openToClaim,
            WHero newOwner,
            WHero oldOwner,
            WHero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail
        )
        {
            try
            {
                if (newOwner?.Base?.Clan == null)
                    return;

                var playerClan = Player.Clan;
                if (playerClan?.Base == null)
                    return;

                if (newOwner.Base.Clan != playerClan.Base)
                    return;

                // Event-driven unlock: should popup.
                TryUnlockClanTroops(playerClan, fromBootstrap: false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        /// <summary>
        /// Handles kingdom creation events to unlock kingdom troops when applicable.
        /// </summary>
        protected override void OnKingdomCreated(Kingdom kingdom)
        {
            try
            {
                var playerKingdom = Player.Kingdom;
                if (playerKingdom?.Base == null)
                    return;

                if (kingdom != playerKingdom.Base)
                    return;

                // Event-driven unlock: should popup.
                TryUnlockKingdomTroops(playerKingdom, fromBootstrap: false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
