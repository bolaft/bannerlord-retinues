using System;
using System.Collections.Generic;
using System.Reflection;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
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
            if (replaceAllCustom)
            {
                // Detach all root custom troop definitions from their factions before
                // sanitizing rosters. Without this, the mod's daily-tick behaviors
                // (PartySwapBehavior, VolunteerSwapOnUpdate) will immediately
                // re-populate custom troops into rosters and volunteer slots after
                // the sanitizer clears them, causing the save to still contain custom
                // troop IDs when the user saves and then uninstalls the mod.
                PurgeCustomTroopDefinitions();
            }

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

        /// <summary>
        /// Calls <see cref="WCharacter.Remove"/> on every root custom troop in the game,
        /// detaching them from their faction objects and removing them from
        /// <see cref="WCharacter.ActiveStubIds"/>. This prevents the mod's party-swap and
        /// volunteer-swap behaviors from re-introducing custom troops after a purge.
        /// </summary>
        private static void PurgeCustomTroopDefinitions()
        {
            try
            {
                // Snapshot before mutation — Remove() modifies ActiveStubIds recursively.
                var snapshot = new List<string>(WCharacter.ActiveStubIds);

                foreach (var id in snapshot)
                {
                    // May already have been removed by a recursive Remove() call.
                    if (!WCharacter.ActiveStubIds.Contains(id))
                        continue;

                    var troop = WCharacter.FromStringId(id);
                    if (troop?.IsCustom != true)
                        continue;

                    // Only remove root troops here; Remove() recurses into upgrade targets.
                    if (troop.Parent != null)
                        continue;

                    troop.Remove();
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "PurgeCustomTroopDefinitions failed.");
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
