using System.Collections.Generic;
using System.Linq;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Core.Game.Events
{
    [SafeClass]
    public class Battle : Combat
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public Battle()
        {
            try
            {
                // Initialize counts
                PlayerTroopCount = Player.Party?.MemberRoster?.Count ?? 0;
                EnemyTroopCount = GetRosters(EnemySide).Sum(r => r.Count);
                AllyTroopCount = GetRosters(PlayerSide).Sum(r => r.Count);
                FriendlyTroopCount = PlayerTroopCount + AllyTroopCount;
                TotalTroopCount = PlayerTroopCount + EnemyTroopCount + AllyTroopCount;

                // Prisoners
                EnemyPrisoners =
                [
                    .. GetRosters(EnemySide, prisoners: true)
                        .SelectMany(r => r.Elements)
                        .Select(e => e.Troop)
                        .Where(t => t != null),
                ];

                // Initialize army status
                PlayerIsInArmy = Player.Party?.IsInArmy ?? false;
                AllyIsInArmy = PartiesOnSide(PlayerSide).Any(p => p?.IsInArmy ?? false);
                EnemyIsInArmy = PartiesOnSide(EnemySide).Any(p => p?.IsInArmy ?? false);

                // Leaders
                EnemyLeaders = GetLeaders(EnemySide);
                AllyLeaders =
                [
                    .. GetLeaders(PlayerSide).Where(l => l?.StringId != Player.Character?.StringId),
                ];
            }
            catch (System.Exception e)
            {
                Log.Exception(e);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Info                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━ Leaders ━━━━━━━ */

        public List<WCharacter> AllyLeaders = [];
        public List<WCharacter> EnemyLeaders = [];

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        public bool IsWon =>
            MapEvent != null
            && PlayerSide != BattleSideEnum.None
            && MapEvent.WinningSide == PlayerSide;
        public bool IsLost => !IsWon;

        public bool IsFieldBattle => MapEvent?.IsFieldBattle == true;
        public bool IsHideout => MapEvent?.IsHideoutBattle == true;
        public bool IsSiege => MapEvent?.IsSiegeAssault == true;
        public bool IsVillageRaid =>
            MapEvent != null
            && !MapEvent.IsFieldBattle
            && MapEvent.MapEventSettlement?.IsVillage == true
            && !IsSiege;

        public bool PlayerIsDefender => PlayerSide == BattleSideEnum.Defender;

        public bool PlayerIsInArmy;
        public bool AllyIsInArmy;
        public bool EnemyIsInArmy;

        /* ━━━━━━━━ Counts ━━━━━━━━ */

        public int TotalTroopCount;
        public int FriendlyTroopCount;
        public int PlayerTroopCount;
        public int EnemyTroopCount;
        public int AllyTroopCount;

        /* ━━━━━━━━ Parties ━━━━━━━ */

        public List<WParty> EnemyParties => [.. PartiesOnSide(EnemySide)];
        public List<WParty> AllyParties => [.. PartiesOnSide(PlayerSide, includePlayer: false)];

        /* ━━━━━━━ Prisoners ━━━━━━ */

        public List<WCharacter> EnemyPrisoners;

        /* ━━━━━━━━━ Sides ━━━━━━━━ */

        public BattleSideEnum PlayerSide
        {
            get
            {
                if (MapEvent == null || Player.Party == null)
                    return BattleSideEnum.None;

                foreach (var party in PartiesOnSide(BattleSideEnum.Attacker, includePlayer: true))
                    if (party?.StringId == Player.Party.StringId)
                        return BattleSideEnum.Attacker;

                foreach (var party in PartiesOnSide(BattleSideEnum.Defender, includePlayer: true))
                    if (party?.StringId == Player.Party.StringId)
                        return BattleSideEnum.Defender;

                return BattleSideEnum.None;
            }
        }

        public BattleSideEnum EnemySide
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Logging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void LogBattleReport()
        {
            Log.Debug($"--- Battle Report ---");
            Log.Debug($"Outcome: {(IsWon ? "Victory" : "Defeat")}");
            Log.Debug(
                $"Type: {(IsSiege ? "Siege" : IsVillageRaid ? "Village Raid" : IsHideout ? "Hideout" : "Field Battle")}"
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
            Log.Debug($"---------------------");

            LogCombatReport();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ Map Event ━━━━━━ */

        private static MapEvent MapEvent => MobileParty.MainParty?.MapEvent;

        /* ━━━━━━━━ Leaders ━━━━━━━ */

        private List<WCharacter> GetLeaders(BattleSideEnum side)
        {
            return [.. PartiesOnSide(side).Select(p => p?.Leader)];
        }

        /* ━━━━━━━━ Rosters ━━━━━━━ */

        private List<WRoster> GetRosters(BattleSideEnum side, bool prisoners = false)
        {
            var rosters = new List<WRoster>();
            foreach (var p in PartiesOnSide(side))
                rosters.Add(prisoners ? p.PrisonRoster : p.MemberRoster);
            return rosters;
        }

        /* ━━━━━━━━ Parties ━━━━━━━ */

        private IEnumerable<WParty> PartiesOnSide(BattleSideEnum side, bool includePlayer = false)
        {
            if (MapEvent == null)
                yield break;
            foreach (var p in MapEvent.PartiesOnSide(side))
            {
                var mp = p?.Party?.MobileParty;
                if (mp == null)
                    continue;
                if (!includePlayer && Player.Party != null && mp.StringId == Player.Party.StringId)
                    continue;
                yield return new WParty(mp);
            }
        }
    }
}
