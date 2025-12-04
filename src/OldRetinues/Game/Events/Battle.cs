using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Retinues.Game.Wrappers;
using Retinues.Mods;
using Retinues.Utils;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Events
{
    /// <summary>
    /// Battle event wrapper, provides helpers for troop counts, sides, leaders, parties, prisoners, and battle reporting.
    /// </summary>
    [SafeClass]
    public class Battle : Combat
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Reflection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly PropertyInfo MissionIsNavalBattleProperty =
            typeof(Mission).GetProperty(
                "IsNavalBattle",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

        private static readonly PropertyInfo MapEventIsNavalMapEventProperty =
            typeof(MapEvent).GetProperty(
                "IsNavalMapEvent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public Battle(MapEvent mapEvent = null)
        {
            try
            {
                // If no map event provided, use the player's current map event if any
                MapEvent = mapEvent ?? MobileParty.MainParty?.MapEvent;

                // Initialize counts
                PlayerTroopCount = Player.Party?.MemberRoster?.HealthyCount ?? 0;
                EnemyTroopCount = GetRosters(EnemySide).Sum(r => r.HealthyCount);
                AllyTroopCount = GetRosters(PlayerSide).Sum(r => r.HealthyCount);
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
                AllyLeaders = [.. GetLeaders(PlayerSide).Where(l => l != Player.Character)];
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
        public bool IsNavalBattle
        {
            get
            {
                try
                {
                    if (ModCompatibility.HasNavalDLC == false)
                        return false;

                    // Mission-side flag (BL13 + War Sails)
                    var mission = Mission.Current;
                    if (mission != null && MissionIsNavalBattleProperty != null)
                    {
                        if (
                            MissionIsNavalBattleProperty.GetValue(mission) is bool missionFlag
                            && missionFlag
                        )
                            return true;
                    }

                    // MapEvent-side flag (BL13 + War Sails)
                    if (MapEvent != null && MapEventIsNavalMapEventProperty != null)
                    {
                        if (
                            MapEventIsNavalMapEventProperty.GetValue(MapEvent) is bool mapFlag
                            && mapFlag
                        )
                            return true;
                    }
                }
                catch (System.Exception e)
                {
                    Log.Exception(e);
                }

                return false;
            }
        }

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
        public List<WParty> AllParties =>
            [.. PartiesOnSide(BattleSideEnum.Attacker), .. PartiesOnSide(BattleSideEnum.Defender)];
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
                    if (party == Player.Party)
                        return BattleSideEnum.Attacker;

                foreach (var party in PartiesOnSide(BattleSideEnum.Defender, includePlayer: true))
                    if (party == Player.Party)
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

        /// <summary>
        /// Log a summary report of the battle outcome, type, sides, counts, leaders, and army status.
        /// </summary>
        public void LogReport()
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
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━ Map Event ━━━━━━ */

        private readonly MapEvent MapEvent;

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

        public IEnumerable<WParty> PartiesOnSide(BattleSideEnum side, bool includePlayer = false)
        {
            if (MapEvent == null)
                yield break;

            // 1) Primary path: engine helper (fastest when stable)
            IEnumerable<MapEventParty> raw = null;
            try
            {
                raw = MapEvent.PartiesOnSide(side);
            }
            catch (System.IndexOutOfRangeException)
            {
                // 2) Fallback path: direct side bag (safer during FinalizeEventAux teardown)
                var mapSide =
                    side == BattleSideEnum.Attacker ? MapEvent.AttackerSide : MapEvent.DefenderSide;
                raw = mapSide?.Parties ?? Enumerable.Empty<MapEventParty>();
            }
            catch
            {
                raw = [];
            }

            foreach (var p in raw)
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
