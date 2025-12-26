using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace OldRetinues.Features.Swaps
{
    [SafeClass]
    public class PartySwapBehavior : CampaignBehaviorBase
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
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnDailyTickParty(MobileParty party)
        {
            if (party == null)
                return;

            var p = new WParty(party);

            // Only enforce for the party kinds we own.
            if (!p.IsMilitia && !p.IsVillager && !p.IsCaravan)
                return;

            var f = p.PlayerFaction;
            if (f == null)
                return; // Not player faction.

            try
            {
                // Caravans: must preserve heroes.
                if (p.IsCaravan)
                {
                    p.MemberRoster?.SwapTroopsPreservingHeroes(f);
                    return;
                }

                // Villagers & militia: no hero leader roster concerns; use the regular swap.
                p.MemberRoster?.SwapTroops(f);
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex, $"PartySwapBehavior failed for {p.Name}");
            }
        }
    }
}
