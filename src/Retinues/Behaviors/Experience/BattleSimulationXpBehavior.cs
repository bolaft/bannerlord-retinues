using System;
using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using TaleWorlds.CampaignSystem.MapEvents;

namespace Retinues.Behaviors.Experience
{
    /// <summary>
    /// Awards skill-point XP to player-side custom troops after auto-resolved (simulated) battles.
    ///
    /// Vanilla only routes non-hero troop XP through TroopRoster.AddXpToTroopAtIndex for *manual*
    /// (mission) combat. Simulated kills go through DefaultSkillLevelingManager.OnSimulationCombatKill,
    /// which rewards heroes only and never touches the troop roster, so the AddXpToTroopAtIndex hook
    /// in ExperiencePatches cannot see auto-resolve XP. This behavior synthesizes it: snapshot the
    /// player/enemy line-up at MapEvent start, then pay custom troops an enemy-tier budget on end.
    /// </summary>
    public sealed class BattleSimulationXpBehavior : BaseCampaignBehavior
    {
        // XP weight per enemy "tier unit" (count * (tier + 1)).
        private const float XpPerTier = 2.5f;

        // Snapshots keyed by the live MapEvent, taken at start (before casualties).
        private readonly Dictionary<MapEvent, Snapshot> _snapshots = [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Start                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnMapEventStarted(MMapEvent mapEvent)
        {
            var me = mapEvent?.Base;
            if (me == null || !mapEvent.IsPlayerInvolved)
                return;

            var playerSide = mapEvent.PlayerSide;
            var enemySide = mapEvent.EnemySide;
            if (playerSide == null || enemySide == null)
                return;

            // Player side: collect custom troops (to credit) and total head-count (to split the budget).
            var customElements = new List<(string TroopId, int Count)>();
            int playerTotalCount = 0;
            bool mainPartyInvolved = false;

            foreach (var party in playerSide.Parties)
            {
                if (party == null)
                    continue;
                if (party.IsMainParty)
                    mainPartyInvolved = true;

                foreach (var e in party.MemberRoster.Elements)
                {
                    if (e.Number <= 0)
                        continue;
                    var troop = e.Troop;
                    if (troop == null)
                        continue;

                    playerTotalCount += e.Number;
                    if (troop.IsCustom)
                        customElements.Add((troop.StringId, e.Number));
                }
            }

            if (customElements.Count == 0 || playerTotalCount <= 0)
                return; // No custom troops on the player side, nothing to credit.

            // Enemy side: budget = sum(count * (tier + 1)).
            int enemyBudgetUnits = 0;
            foreach (var party in enemySide.Parties)
            {
                if (party == null)
                    continue;
                foreach (var e in party.MemberRoster.Elements)
                {
                    if (e.Number <= 0)
                        continue;
                    var troop = e.Troop;
                    if (troop == null)
                        continue;
                    int tier = Math.Max(0, troop.Tier);
                    enemyBudgetUnits += e.Number * (tier + 1);
                }
            }

            if (enemyBudgetUnits <= 0)
                return;

            _snapshots[me] = new Snapshot
            {
                MainPartyInvolved = mainPartyInvolved,
                PlayerTotalCount = playerTotalCount,
                EnemyBudgetUnits = enemyBudgetUnits,
                CustomElements = customElements,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           End                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnMapEventEnded(MMapEvent mapEvent)
        {
            var me = mapEvent?.Base;
            if (me == null)
                return;

            if (!_snapshots.TryGetValue(me, out var snap))
                return;
            _snapshots.Remove(me);

            // Manual player battles award XP through the mission AddXpToTroopAtIndex hook; only handle
            // simulated (auto-resolve) outcomes here so we never double-count.
            if (snap.MainPartyInvolved && !me.IsPlayerSimulation)
                return;

            float budget = snap.EnemyBudgetUnits * XpPerTier;
            if (budget <= 0f || snap.PlayerTotalCount <= 0)
                return;

            var party = Player.Party?.PartyBase;

            foreach (var (troopId, count) in snap.CustomElements)
            {
                int share = (int)Math.Round(budget * (double)count / snap.PlayerTotalCount);
                if (share <= 0)
                    continue;

                var wc = WCharacter.Get(troopId);
                if (wc == null)
                    continue;

                // Feeds the same skill-point conversion the manual-combat XP hook uses.
                SkillPointExperienceGain.ApplyXpToSkillPointProgress(wc, party, share);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private sealed class Snapshot
        {
            public bool MainPartyInvolved;
            public int PlayerTotalCount;
            public int EnemyBudgetUnits;
            public List<(string TroopId, int Count)> CustomElements;
        }
    }
}
