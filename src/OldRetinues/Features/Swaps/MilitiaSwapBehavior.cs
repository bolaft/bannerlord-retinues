using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Features.Swaps
{
    [SafeClass]
    public class MilitiaSwapBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(
                this,
                OnDailyTickSettlement
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (settlement == null)
                return;

            var s = new WSettlement(settlement);
            var f = s.PlayerFaction;

            if (f == null)
                return; // Not player faction

            Log.Debug(
                $"MilitiaSwap: Daily tick for {settlement?.Name} ({f?.Name ?? "not player faction"})."
            );

            // Swap militias
            s.MilitiaParty?.MemberRoster?.SwapTroops(f);
        }
    }
}
