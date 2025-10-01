using System;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Core.Safety
{
    public class SafetyBehavior : CampaignBehaviorBase
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
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnGameLoaded(CampaignGameStarter _)
        {
            Log.Info("Performing safety checks...");

            try
            {
                foreach (var mp in MobileParty.All)
                    RosterSanitizer.CleanParty(mp);

                foreach (var s in Campaign.Current.Settlements)
                {
                    RosterSanitizer.CleanParty(s?.Town?.GarrisonParty);
                    RosterSanitizer.CleanParty(s?.MilitiaPartyComponent?.MobileParty);

                    VolunteerSanitizer.CleanSettlement(s);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
