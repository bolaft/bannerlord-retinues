using System.Collections.Generic;
using Retinues.Behaviors.Doctrines.Catalogs;
using Retinues.Behaviors.Missions;
using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using Retinues.Interface.Services;
using TaleWorlds.Core;

namespace Retinues.Behaviors.Retinues
{
    [SafeClass]
    public sealed class SurvivalChanceBehavior : BaseCampaignBehavior
    {
        const float ImmortalsSurvivalChance = 0.2f;

        public override bool IsActive => Settings.EnableRetinues;

        /// <summary>
        /// Restores a chance of fallen retinues as wounded after mission ends.
        /// </summary>
        protected override void OnMissionEnded(MMission mission)
        {
            if (DoctrineCatalog.Immortals.IsAcquired == false)
                return; // No reason to proceed.

            Dictionary<WCharacter, int> casualties = [];

            // Count each retinue troop's deaths
            foreach (var kill in CombatBehavior.GetKills())
            {
                if (!kill.Victim.IsPlayerTroop)
                    continue; // Only consider player troops

                if (kill.State != AgentState.Killed)
                    continue; // Only consider killed troops

                WCharacter victim = kill.Victim.Character;

                if (!victim.IsRetinue)
                    continue; // Only consider retinues

                if (!casualties.ContainsKey(victim))
                    casualties[victim] = 0;

                casualties[victim]++;
            }

            if (casualties.Count == 0)
                return; // No retinue casualties to process.

            // Get the player's party roster
            var roster = Player.Party.MemberRoster;

            // Restore a portion of dead retinues as wounded
            foreach (var kvp in casualties)
            {
                var troop = kvp.Key;
                int revived = 0;

                for (int i = 0; i < kvp.Value; i++)
                    if (MBRandom.RandomFloat < ImmortalsSurvivalChance)
                        revived++;

                if (revived > 0)
                {
                    // Add back as wounded survivors
                    roster.AddTroop(
                        troop,
                        0,
                        woundedNumber: revived // Add as wounded
                    );
                    Notifications.Message(
                        L.T(
                                "immortals_restored_message",
                                "Immortals: {REVIVED} of {TOTAL} fallen {TROOP} survived grievous wounds."
                            )
                            .SetTextVariable("REVIVED", revived)
                            .SetTextVariable("TOTAL", kvp.Value)
                            .SetTextVariable("TROOP", troop.Name)
                    );
                }
            }
        }
    }
}
