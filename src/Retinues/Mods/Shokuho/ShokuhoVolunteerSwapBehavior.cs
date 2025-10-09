using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Mods.Shokuho
{
    /// <summary>
    /// Campaign behavior for Shokuho mod compatibility.
    /// Swaps volunteers in settlements on daily tick using WSettlement wrapper.
    /// </summary>
    public sealed class ShokuhoVolunteerSwapBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// No sync data needed for Shokuho volunteer swap behavior.
        /// </summary>
        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers daily tick event listener for settlements to swap volunteers.
        /// </summary>
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(
                this,
                DailyTickSettlement
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Swaps volunteers in the settlement using WSettlement wrapper.
        /// </summary>
        private void DailyTickSettlement(Settlement settlement)
        {
            var s = new WSettlement(settlement);
            s.SwapVolunteers();
        }
    }
}
