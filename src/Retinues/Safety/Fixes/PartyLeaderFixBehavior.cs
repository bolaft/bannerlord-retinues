using System.Collections.Generic;
using System.Linq;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Retinues.Safety.Fixes
{
    /// <summary>
    /// Safety helpers for repairing common issues in the game state.
    /// </summary>
    [SafeClass]
    public class PartyLeaderFixBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void SyncData(IDataStore dataStore)
        {
            throw new System.NotImplementedException();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

        private void OnGameLoadFinished()
        {
            FixPartyLeaders();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensures that the main party has the correct leader assigned.
        /// </summary>
        public static void FixPartyLeaders()
        {
            foreach (var party in MobileParty.All)
            {
                if (party == null)
                    continue; // Should not happen

                if (party.LeaderHero != null)
                    continue; // All good

                if (party.IsMainParty)
                    EnsurePartyLeader(party, Hero.MainHero);

                else if (party.IsLordParty)
                    EnsurePartyLeader(party);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ensures the given party has a valid leader assigned.
        /// </summary>
        private static void EnsurePartyLeader(MobileParty party, Hero hero = null)
        {
            Log.Warn($"Party {party.Name} has no leader, attempting to assign one.");

            hero ??= FindHeroLeader(party);

            if (hero != null)
                party.PartyComponent.ChangePartyLeader(hero);
        }

        /// <summary>
        /// Finds a suitable hero to assign as the leader of the given party.
        /// </summary>
        private static Hero FindHeroLeader(MobileParty party)
        {
            // List to hold all heroes found in the party
            List<Hero> heroes = [];

            // Gather all heroes in the party
            foreach (var member in party.MemberRoster.GetTroopRoster())
                if (member.Character?.IsHero == true && member.Character?.HeroObject != null)
                    heroes.Add(member.Character.HeroObject);

            // Leader hero to be determined
            Hero leader = null;

            if (heroes.Count == 0)
            {
                // No heroes found
                Log.Warn($"No heroes found in party {party.Name}");
            }
            else if (heroes.Count == 1)
            {
                // Single hero found, select as leader
                leader = heroes[0];
            }
            else
            {
                Log.Info($"Multiple heroes found in party {party.Name}.");

                // Multiple heroes found, try to find a lord
                List<Hero> lords = [.. heroes.Where(h => h.IsLord)];

                if (lords.Count == 1)
                    leader = lords[0];
                else if (lords.Count > 1)
                    Log.Warn($"Multiple lord heroes found in party, cannot select leader.");
            }

            if (leader != null)
                Log.Info($"Selecting hero {leader.Name} as leader of party {party.Name}.");
            else
                Log.Warn($"Failed to find leader for party {party.Name}.");

            return leader;
        }
    }
}
