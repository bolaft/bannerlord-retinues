using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Retinues.Features.Experience
{
    /// <summary>
    /// Awards XP to custom player-side troops after simulated (auto-resolve) battles.
    /// Takes a snapshot at MapEvent start (counts/tiers before finalization) and pays on end.
    /// </summary>
    [SafeClass]
    public class BattleSimulationXpBehavior : CampaignBehaviorBase
    {
        private const float XpPerTier = 2.5f; // enemy-only tier weighting

        // Snapshots keyed by MapEvent reference
        private readonly Dictionary<MapEvent, Snapshot> _snapshots = [];

        public override void RegisterEvents()
        {
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore dataStore) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Start                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [SafeMethod]
        private void OnMapEventStarted(
            MapEvent me,
            PartyBase attackerParty,
            PartyBase defenderParty
        )
        {
            if (me == null)
                return;

            // Build raw sides from wrapper without relying on PlayerSide (works when player absent)
            var battle = new Battle(me);
            var attackers = battle
                .PartiesOnSide(BattleSideEnum.Attacker, includePlayer: true)
                .ToList();
            var defenders = battle
                .PartiesOnSide(BattleSideEnum.Defender, includePlayer: true)
                .ToList();

            // Decide where the player ownership is (main party or any player-faction party)
            bool playerOnAtk = attackers.Any(p => p.IsMainParty || p.PlayerFaction != null);
            bool playerOnDef = defenders.Any(p => p.IsMainParty || p.PlayerFaction != null);
            if (!playerOnAtk && !playerOnDef)
            {
                // No player-owned parties, ensure no stale snapshot remains
                _snapshots.Remove(me);
                return;
            }

            bool mainPartyInvolved =
                me.InvolvedParties?.Any(p => p?.MobileParty?.IsMainParty == true) == true;

            // Choose sides relative to player
            var playerSide = playerOnAtk ? attackers : defenders;
            var enemySide = playerOnAtk ? defenders : attackers;

            // Snapshot counts for player-side (all troops; heroes optional) and enemy-side (for budget)
            var playerElems = new List<(BasicCharacterObject Troop, int Count, bool IsCustom)>();
            int playerTotalCount = 0;

            foreach (var wp in playerSide)
            {
                foreach (var e in wp.MemberRoster.Elements)
                {
                    if (e.Number <= 0 || e.Troop == null)
                        continue;

                    playerElems.Add((e.Troop.Base, e.Number, e.Troop.IsCustom));
                    playerTotalCount += e.Number;
                }
            }

            // Enemy budget units = sum(count * (tier+1)); convert to XP later with XpPerTier
            int enemyBudgetUnits = 0;
            foreach (var ep in enemySide)
            {
                foreach (var e in ep.MemberRoster.Elements)
                {
                    if (e.Number <= 0 || e.Troop == null)
                        continue;
                    int tier = Math.Max(0, e.Troop.Tier);
                    enemyBudgetUnits += e.Number * (tier + 1);
                }
            }

            _snapshots[me] = new Snapshot
            {
                MainPartyInvolved = mainPartyInvolved,
                PlayerOnAttack = playerOnAtk,
                PlayerTotalCount = playerTotalCount,
                PlayerElements = playerElems,
                EnemyBudgetUnits = enemyBudgetUnits,
            };

            Log.Debug(
                $"AutoResolveXP[Start]: playerInvolved={mainPartyInvolved}, atk={attackers.Count}, def={defenders.Count}, playerCount={playerTotalCount}, enemyUnits={enemyBudgetUnits}"
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         End                            //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [SafeMethod]
        private void OnMapEventEnded(MapEvent me)
        {
            if (me == null)
                return;
            if (!_snapshots.TryGetValue(me, out var snap))
            {
                // No snapshot
                return;
            }
            _snapshots.Remove(me);

            // Only for simulated outcomes, real battles are handled by mission behavior.
            if (snap.MainPartyInvolved && !me.IsPlayerSimulation)
            {
                Log.Debug("AutoResolveXP[End]: real battle, skipping.");
                return;
            }

            // Budget from enemy snapshot
            float budget = snap.EnemyBudgetUnits * XpPerTier;
            if (budget <= 0 || snap.PlayerTotalCount <= 0)
            {
                Log.Debug(
                    $"AutoResolveXP[End]: budget={budget}, playerTotal={snap.PlayerTotalCount} → nothing to award."
                );
                return;
            }

            // Distribute by ALL player-side counts from START snapshot, but only credit customs
            foreach (var (Troop, Count, IsCustom) in snap.PlayerElements)
            {
                int share = (int)Math.Round(budget * (double)Count / snap.PlayerTotalCount);
                if (share <= 0)
                    continue;

                if (IsCustom)
                    TroopXpBehavior.Add(new WCharacter(Troop.StringId), share);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Helpers                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private sealed class Snapshot
        {
            public bool MainPartyInvolved;
            public bool PlayerOnAttack;
            public int PlayerTotalCount;
            public int EnemyBudgetUnits;
            public List<(BasicCharacterObject Troop, int Count, bool IsCustom)> PlayerElements;
        }
    }
}
