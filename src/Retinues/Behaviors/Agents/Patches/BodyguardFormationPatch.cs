using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace Retinues.Behaviors.Agents.Patches
{
    /// <summary>
    /// Forces all AI-clan retinue agents into their team's Bodyguard formation.
    ///
    /// Player-clan retinues are intentionally excluded so that the player retains manual
    /// control over their formation placement.
    /// </summary>
    [HarmonyPatch(
        typeof(GeneralsAndCaptainsAssignmentLogic),
        nameof(GeneralsAndCaptainsAssignmentLogic.OnTeamDeployed)
    )]
    internal static class BodyguardFormationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Team team)
        {
            try
            {
                // Only AI teams — never touch player-team retinues.
                if (team == null || team.IsPlayerTeam)
                    return;

                // Only act when vanilla already set up a Bodyguard formation.
                // (Field battles: yes. Siege battles: createBodyguard=false, so null here.)
                var bodyguard = team.BodyGuardFormation;
                if (bodyguard == null)
                    return;

                // Collect retinue agents that are not already in the bodyguard formation.
                var toMove = new List<Agent>();
                var dirtyFormations = new HashSet<Formation>();

                foreach (var formation in team.FormationsIncludingEmpty)
                {
                    if (formation == bodyguard)
                        continue;

                    // Materialise to avoid mutating during enumeration.
                    var units = formation.UnitsWithoutLooseDetachedOnes.ToList();
                    foreach (var unit in units)
                    {
                        if (unit is not Agent agent)
                            continue;

                        if (agent.Character is not CharacterObject co)
                            continue;

                        var wc = WCharacter.Get(co);
                        if (wc == null || !wc.IsRetinue)
                            continue;

                        toMove.Add(agent);
                        dirtyFormations.Add(formation);
                    }
                }

                if (toMove.Count == 0)
                    return;

                // Reassign each retinue to the bodyguard formation.
                foreach (var agent in toMove)
                    agent.Formation = bodyguard;

                // Expire query caches on formations that lost units.
                foreach (var f in dirtyFormations)
                {
                    team.TriggerOnFormationsChanged(f);
                    f.QuerySystem.Expire();
                }

                bodyguard.QuerySystem.Expire();
                team.TriggerOnFormationsChanged(bodyguard);

                Log.Debug(
                    $"[BodyguardFormation] Moved {toMove.Count} retinue(s) to Bodyguard formation for team '{team.Side}'."
                );
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "BodyguardFormationPatch.Postfix failed.");
            }
        }
    }
}
