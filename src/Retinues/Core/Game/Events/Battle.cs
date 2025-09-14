using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Events
{
    public class Battle : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        // =========================================================================
        // Info
        // =========================================================================

        // -------- Kills --------

        public readonly List<Kill> Kills = [];

        // -------- Leaders --------

        public List<WCharacter> AllyLeaders => [.. GetLeaders(PlayerSide).Where(l => l.StringId != Player.Character.StringId)];
        public List<WCharacter> EnemyLeaders => GetLeaders(EnemySide);

        // -------- Flags --------

        public bool IsWon => PlayerSide != BattleSideEnum.None && MapEvent.WinningSide == PlayerSide;
        public bool IsLost => !IsWon;

        public bool IsSiege => MapEvent.IsSiegeAssault || MapEvent.IsSiegeAmbush || MapEvent.IsSiegeOutside;
        public bool IsVillageRaid => MapEvent.MapEventSettlement != null && MapEvent.MapEventSettlement.IsVillage && !IsSiege;

        public bool PlayerIsDefender => PlayerSide == BattleSideEnum.Defender;

        public bool PlayerIsInArmy => Player.Party.IsInArmy;
        public bool AllyIsInArmy => PartiesOnSide(PlayerSide).Any(p => p.IsInArmy);
        public bool EnemyIsInArmy => PartiesOnSide(EnemySide).Any(p => p.IsInArmy);

        // -------- Counts --------

        public int TotalTroopCount => PlayerTroopCount + EnemyTroopCount + AllyTroopCount;
        public int FriendlyTroopCount => PlayerTroopCount + AllyTroopCount;
        public int PlayerTroopCount => Player.Party.MemberRoster.Count;
        public int EnemyTroopCount => GetRosters(EnemySide).Sum(r => r.Count);
        public int AllyTroopCount => GetRosters(PlayerSide).Sum(r => r.Count);

        // -------- Prisoners --------

        public List<WCharacter> EnemyPrisoners => [.. GetRosters(EnemySide, prisoners: true).SelectMany(r => r.Elements).Select(e => e.Troop).Where(t => t != null)];

        // =========================================================================
        // Mission Events
        // =========================================================================

        public override void OnAgentRemoved(Agent victim, Agent killer, AgentState state, KillingBlow blow)
        {
            if (victim == null || killer == null)
                return; // e.g. if agent despawned

            if (state != AgentState.Killed && state != AgentState.Unconscious)
                return; // only care about kills and knockouts

            Kills.Add(new Kill(victim, killer, state, blow));
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
        }

        // -------- Sides --------

        private BattleSideEnum PlayerSide
        {
            get
            {
                foreach (var party in PartiesOnSide(BattleSideEnum.Attacker, includePlayer: true))
                    if (party.StringId == Player.Party.StringId) return BattleSideEnum.Attacker;

                foreach (var party in PartiesOnSide(BattleSideEnum.Defender, includePlayer: true))
                    if (party.StringId == Player.Party.StringId) return BattleSideEnum.Defender;

                return BattleSideEnum.None;
            }
        }

        private BattleSideEnum EnemySide
        {
            get
            {
                if (PlayerSide == BattleSideEnum.Attacker) return BattleSideEnum.Defender;
                if (PlayerSide == BattleSideEnum.Defender) return BattleSideEnum.Attacker;
                return BattleSideEnum.None;
            }
        }

        // -------- Leaders --------

        private List<WCharacter> GetLeaders(BattleSideEnum side)
        {
            return [.. PartiesOnSide(side).Select(p => p.Leader)];
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
                if (mp == null) continue;
                if (!includePlayer && mp.StringId == Player.Party.StringId) continue; // skip player party
                yield return new WParty(mp);
            }
        }

        // =========================================================================
        // Logging
        // =========================================================================

        public void LogReport()
        {
            Log.Debug($"--- Battle Report ---");
            Log.Debug($"Type: {(IsSiege ? "Siege" : IsVillageRaid ? "Village Raid" : "Field Battle")}");
            Log.Debug($"Sides: Player is {PlayerSide}, Enemy is {EnemySide}");
            Log.Debug($"Counts: Player={PlayerTroopCount}, Allies={AllyTroopCount}, Enemies={EnemyTroopCount}, Total={TotalTroopCount}");
            Log.Debug($"Kills: {Kills.Count} total");
            Log.Debug($"PlayerKills = {Kills.Where(k => k.Killer.IsPlayer).Count()}");
            Log.Debug($"CustomKills = {Kills.Where(k => k.Killer.IsPlayerTroop && k.Killer.Character.IsCustom).Count()}");
            Log.Debug($"RetinueKills = {Kills.Where(k => k.Killer.IsPlayerTroop && k.Killer.Character.IsRetinue).Count()}");
            Log.Debug($"---------------------");
        }
    }
}