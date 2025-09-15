using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Events
{
    public class Battle : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public Battle()
        {
            // Initialize counts
            PlayerTroopCount = Player.Party?.MemberRoster?.Count ?? 0;
            EnemyTroopCount = GetRosters(EnemySide).Sum(r => r.Count);
            AllyTroopCount = GetRosters(PlayerSide).Sum(r => r.Count);
            FriendlyTroopCount = PlayerTroopCount + AllyTroopCount;
            TotalTroopCount = PlayerTroopCount + EnemyTroopCount + AllyTroopCount;

            // Prisoners
            EnemyPrisoners = GetRosters(EnemySide, prisoners: true)
                .SelectMany(r => r.Elements)
                .Select(e => e.Troop)
                .Where(t => t != null)
                .ToList();

            // Initialize army status
            PlayerIsInArmy = Player.Party?.IsInArmy ?? false;
            AllyIsInArmy = PartiesOnSide(PlayerSide).Any(p => p?.IsInArmy ?? false);
            EnemyIsInArmy = PartiesOnSide(EnemySide).Any(p => p?.IsInArmy ?? false);

            // Leaders
            try
            {
                EnemyLeaders = GetLeaders(EnemySide);
                AllyLeaders =
                [
                    .. GetLeaders(PlayerSide).Where(l => l?.StringId != Player.Character?.StringId),
                ];
            }
            catch (System.Exception e)
            {
                Log.Exception(e);
                EnemyLeaders = [];
                AllyLeaders = [];
            }
        }

        // =========================================================================
        // Info
        // =========================================================================

        // -------- Kills --------

        public readonly List<Kill> Kills = [];

        // -------- Leaders --------

        public List<WCharacter> AllyLeaders;
        public List<WCharacter> EnemyLeaders;

        // -------- Flags --------

        public bool IsWon =>
            PlayerSide != BattleSideEnum.None && MapEvent.WinningSide == PlayerSide;
        public bool IsLost => !IsWon;

        public bool IsSiege =>
            MapEvent.IsSiegeAssault || MapEvent.IsSiegeAmbush || MapEvent.IsSiegeOutside;
        public bool IsVillageRaid =>
            !MapEvent.IsFieldBattle
            && MapEvent.MapEventSettlement != null
            && MapEvent.MapEventSettlement.IsVillage
            && !IsSiege;

        public bool PlayerIsDefender => PlayerSide == BattleSideEnum.Defender;

        public bool PlayerIsInArmy;
        public bool AllyIsInArmy;
        public bool EnemyIsInArmy;

        // -------- Counts --------

        public int TotalTroopCount;
        public int FriendlyTroopCount;
        public int PlayerTroopCount;
        public int EnemyTroopCount;
        public int AllyTroopCount;

        // -------- Prisoners --------

        public List<WCharacter> EnemyPrisoners;

        // =========================================================================
        // Mission Events
        // =========================================================================

        public override void OnAgentRemoved(
            Agent victim,
            Agent killer,
            AgentState state,
            KillingBlow blow
        )
        {
            if (victim == null || killer == null)
                return; // e.g. if agent despawned

            if (state != AgentState.Killed && state != AgentState.Unconscious)
                return; // only care about kills and knockouts

            if (victim.Character is not CharacterObject)
                return; // ignore non-character agents (horses, etc)

            if (killer.Character is not CharacterObject)
                return; // ignore non-character agents (horses, etc)

            Kills.Add(new Kill(victim, killer, state, blow));
            // Kills.Last().Report();
        }

        // =========================================================================
        // Internals
        // =========================================================================

        // -------- Map Event --------

        private static MapEvent MapEvent => MobileParty.MainParty?.MapEvent;

        // -------- Kills --------

        public class Kill(Agent victim, Agent killer, AgentState state, KillingBlow blow)
        {
            public WAgent Victim = new(victim);
            public WAgent Killer = new(killer);
            public AgentState State = state;
            public KillingBlow Blow = blow;

            public void Report()
            {
                string GetAgentType(WAgent agent)
                {
                    if (agent.Character == null)
                        return "NoChar";
                    if (agent.IsPlayer)
                        return "Player";
                    if (agent.Character.IsCustom)
                        return "Custom";
                    if (agent.Character.IsRetinue)
                        return "Retinue";
                    return "Vanilla";
                }
                var victimType = GetAgentType(Victim);
                var killerType = GetAgentType(Killer);
                var method = Blow.IsMissile ? "Missile" : "Melee";
                var headshot = Blow.IsHeadShot() ? "[Headshot]" : "";
                var faction = Killer.Character?.Faction?.Name ?? "No Faction";
                Log.Info(
                    $"  {State}: {Victim.Character.Name} ({victimType}) killed by {Killer.Character.Name} [{faction}] ({killerType}) via {method} {headshot}"
                );
            }
        }

        // -------- Sides --------

        private BattleSideEnum PlayerSide
        {
            get
            {
                foreach (var party in PartiesOnSide(BattleSideEnum.Attacker, includePlayer: true))
                    if (party.StringId == Player.Party.StringId)
                        return BattleSideEnum.Attacker;

                foreach (var party in PartiesOnSide(BattleSideEnum.Defender, includePlayer: true))
                    if (party.StringId == Player.Party.StringId)
                        return BattleSideEnum.Defender;

                return BattleSideEnum.None;
            }
        }

        private BattleSideEnum EnemySide
        {
            get
            {
                if (PlayerSide == BattleSideEnum.Attacker)
                    return BattleSideEnum.Defender;
                if (PlayerSide == BattleSideEnum.Defender)
                    return BattleSideEnum.Attacker;
                return BattleSideEnum.None;
            }
        }

        // -------- Leaders --------

        private List<WCharacter> GetLeaders(BattleSideEnum side)
        {
            return [.. PartiesOnSide(side).Select(p => p?.Leader)];
        }

        // -------- Rosters --------

        private List<WRoster> GetRosters(BattleSideEnum side, bool prisoners = false)
        {
            var rosters = new List<WRoster>();
            foreach (var p in PartiesOnSide(side))
                rosters.Add(prisoners ? p.PrisonRoster : p.MemberRoster);
            return rosters;
        }

        // -------- Parties --------

        private IEnumerable<WParty> PartiesOnSide(BattleSideEnum side, bool includePlayer = false)
        {
            foreach (var p in MapEvent.PartiesOnSide(side))
            {
                var mp = p?.Party?.MobileParty;
                if (mp == null)
                    continue;
                if (!includePlayer && mp.StringId == Player.Party.StringId)
                    continue; // skip player party
                yield return new WParty(mp);
            }
        }

        // =========================================================================
        // Logging
        // =========================================================================

        public void LogReport()
        {
            Log.Debug($"--- Battle Report ---");
            Log.Debug($"Outcome: {(IsWon ? "Victory" : "Defeat")}");
            Log.Debug(
                $"Type: {(IsSiege ? "Siege" : IsVillageRaid ? "Village Raid" : "Field Battle")}"
            );
            Log.Debug($"Sides: Player is {PlayerSide}, Enemy is {EnemySide}");
            Log.Debug(
                $"Counts: Player={PlayerTroopCount}, Allies={AllyTroopCount}, Enemies={EnemyTroopCount}, Total={TotalTroopCount}"
            );
            Log.Debug(
                $"Enemy Leaders: [{string.Join(", ", EnemyLeaders.Select(l => l?.Name))}], Ally Leaders: [{string.Join(", ", AllyLeaders.Select(l => l?.Name))}]"
            );
            Log.Debug(
                $"Player In Army: {PlayerIsInArmy}, Allies In Army: {AllyIsInArmy}, Enemies In Army: {EnemyIsInArmy}"
            );
            Log.Debug($"Kills: {Kills.Count} total");
            Log.Debug($"PlayerKills = {Kills.Where(k => k.Killer.IsPlayer).Count()}");
            Log.Debug($"CustomKills = {Kills.Where(k => k.Killer.Character.IsCustom).Count()}");
            Log.Debug($"RetinueKills = {Kills.Where(k => k.Killer.Character.IsRetinue).Count()}");
            Log.Debug($"---------------------");
        }
    }
}
