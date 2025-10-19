using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Safety.Sanitizer
{
    /// <summary>
    /// Campaign behavior for running safety checks and sanitizing rosters after game load.
    /// Cleans up invalid party and settlement data to prevent save corruption and crashes.
    /// </summary>
    [SafeClass]
    public class SanitizerBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// No sync data needed for sanitizer behavior.
        /// </summary>
        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Registers event listener for game load finished to trigger safety checks.
        /// </summary>
        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                this,
                OnGameLoadFinished
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Performs safety checks and sanitizes all parties and settlements after game load.
        /// </summary>
        private void OnGameLoadFinished()
        {
            Log.Info("Performing safety checks...");

            Sanitize();
        }

        public static void Sanitize()
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
    }
}
