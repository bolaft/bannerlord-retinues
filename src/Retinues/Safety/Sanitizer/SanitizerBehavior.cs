using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;

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

        public static void Sanitize(bool replaceAllCustom = false)
        {
            foreach (var mp in MobileParty.All)
                PartySanitizer.SanitizeParty(mp, replaceAllCustom);

            foreach (var s in Campaign.Current.Settlements)
            {
                PartySanitizer.SanitizeParty(s?.Town?.GarrisonParty, replaceAllCustom);
                PartySanitizer.SanitizeParty(
                    s?.MilitiaPartyComponent?.MobileParty,
                    replaceAllCustom
                );

                VolunteerSanitizer.SanitizeSettlement(s, replaceAllCustom);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Checks if a CharacterObject is valid and active in the object manager.
        /// </summary>
        public static bool IsCharacterValid(CharacterObject c, bool replaceAllCustom = false)
        {
            if (c == null)
                return false;

            // Wrapper knows how to detect inactive/unregistered TW objects.
            var w = new WCharacter(c);
            if (w?.IsValid != true)
                return false;

            if (replaceAllCustom && (w.IsCustom || w.IsLegacyCustom))
                return false; // Force replace all custom troops

            // Ensure the object manager can resolve it back
            var fromDb = MBObjectManager.Instance?.GetObject<CharacterObject>(c.StringId);
            if (!ReferenceEquals(fromDb, c) && fromDb == null)
                return false;

            return true;
        }
    }
}
