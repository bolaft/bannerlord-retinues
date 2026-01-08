using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Events.Helpers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Framework.Model;
using Retinues.Framework.Runtime;
using Retinues.Game;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;

namespace Retinues.Domain.Events.Models
{
    /// <summary>
    /// Computed wrapper around a MapEvent.
    /// Exposes wrapped parties, leaders, rosters, and derived stats.
    /// </summary>
    public class MMapEvent(MapEvent @base) : MBase<MapEvent>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Current                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [StaticClear]
        public static MMapEvent Current { get; private set; }

        internal static void SetCurrent(MapEvent mapEvent)
        {
            Current = mapEvent != null ? new MMapEvent(mapEvent) : null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Event Type                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public MapEvent.BattleTypes EventType => Base?.EventType ?? default;

        public bool IsFieldBattle
        {
            get
            {
                if (Base == null)
                    return false;

                return !IsSiege && !IsRaid && !IsNavalBattle;
            }
        }

        public bool IsSiege => EventType == MapEvent.BattleTypes.Siege;

        public bool IsRaid => EventType == MapEvent.BattleTypes.Raid;

        public bool IsNavalBattle => NavalBattleHelper.IsNavalBattle(Base);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Outcome                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public enum MapEventOutcome
        {
            Unset,
            Won,
            Lost,
        }

        public MapEventOutcome Outcome
        {
            get
            {
                if (Base == null || !IsPlayerInvolved)
                    return MapEventOutcome.Unset;

                // Battle not resolved yet.
                if (Base.WinningSide == BattleSideEnum.None)
                    return MapEventOutcome.Unset;

                if (PlayerSide == null)
                    return MapEventOutcome.Unset;

                var isPlayerAttacker = ReferenceEquals(PlayerSide, GetAttacker());
                var isPlayerDefender = ReferenceEquals(PlayerSide, GetDefender());

                if (!isPlayerAttacker && !isPlayerDefender)
                    return MapEventOutcome.Unset;

                if (isPlayerAttacker)
                {
                    return Base.WinningSide == BattleSideEnum.Attacker
                        ? MapEventOutcome.Won
                        : MapEventOutcome.Lost;
                }

                // Player is defender
                return Base.WinningSide == BattleSideEnum.Defender
                    ? MapEventOutcome.Won
                    : MapEventOutcome.Lost;
            }
        }

        public bool IsWon => Outcome == MapEventOutcome.Won;
        public bool IsLost => Outcome == MapEventOutcome.Lost;
        public bool IsResolved => Outcome != MapEventOutcome.Unset;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Wrapped Sides                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IReadOnlyList<WParty> DefenderParties => GetParties(GetDefender());
        public IReadOnlyList<WParty> AttackerParties => GetParties(GetAttacker());

        public IReadOnlyList<WParty> PlayerSideParties => GetParties(PlayerSide);
        public IReadOnlyList<WParty> EnemySideParties => GetParties(EnemySide);

        public IReadOnlyList<WHero> DefenderLeaders => GetPartyLeaders(GetDefender());
        public IReadOnlyList<WHero> AttackerLeaders => GetPartyLeaders(GetAttacker());

        public IReadOnlyList<WHero> PlayerSideLeaders => GetPartyLeaders(PlayerSide);
        public IReadOnlyList<WHero> EnemySideLeaders => GetPartyLeaders(EnemySide);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsPlayerInvolved => IsPartyInvolved(Player.Party);

        public bool IsPlayerInArmy => IsPlayerInvolved && Player.Party?.IsInArmy == true;

        public bool IsEnemyAnArmy
        {
            get
            {
                var enemy = EnemySide;
                if (enemy == null)
                    return false;

                foreach (var wp in GetParties(enemy))
                {
                    if (wp.IsInArmy)
                        return true;
                }

                return false;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Troop Counts                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int TotalTroopCount => DefenderTroopCount + AttackerTroopCount;

        public int DefenderTroopCount => GetTroopCount(GetDefender());
        public int AttackerTroopCount => GetTroopCount(GetAttacker());

        public int PlayerTroopCount => Player.Party?.PartySize ?? 0;
        public int AllyTroopCount => GetTroopCount(PlayerSide);
        public int EnemyTroopCount => GetTroopCount(EnemySide);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Strength                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public float TotalStrength => DefenderStrength + AttackerStrength;

        public float DefenderStrength => GetTotalStrength(GetDefender());
        public float AttackerStrength => GetTotalStrength(GetAttacker());

        public float PlayerStrength => Player.Party?.Strength ?? 0f;
        public float AllyStrength => GetTotalStrength(PlayerSide);
        public float EnemyStrength => GetTotalStrength(EnemySide);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Internals                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MapEventSide GetDefender() => Base?.DefenderSide;

        MapEventSide GetAttacker() => Base?.AttackerSide;

        MapEventSide PlayerSide
        {
            get
            {
                if (!IsPlayerInvolved)
                    return null;

                return GetSideOf(Player.Party);
            }
        }

        MapEventSide EnemySide
        {
            get
            {
                if (!IsPlayerInvolved)
                    return null;

                if (PlayerSide == null)
                    return null;

                var d = GetDefender();
                var a = GetAttacker();

                if (d == null || a == null)
                    return null;

                return ReferenceEquals(PlayerSide, d) ? a : d;
            }
        }

        bool IsPartyInvolved(WParty party)
        {
            if (Base == null || party == null)
                return false;

            var d = GetDefender();
            if (d != null)
            {
                foreach (var p in d.Parties)
                {
                    if (ReferenceEquals(p?.Party, party.Base.Party))
                        return true;
                }
            }

            var a = GetAttacker();
            if (a != null)
            {
                foreach (var p in a.Parties)
                {
                    if (ReferenceEquals(p?.Party, party))
                        return true;
                }
            }

            return false;
        }

        MapEventSide GetSideOf(WParty party)
        {
            if (Base == null || party == null)
                return null;

            var d = GetDefender();
            if (d != null)
            {
                foreach (var p in d.Parties)
                {
                    if (ReferenceEquals(p?.Party, party.Base.Party))
                        return d;
                }
            }

            var a = GetAttacker();
            if (a != null)
            {
                foreach (var p in a.Parties)
                {
                    if (ReferenceEquals(p?.Party, party))
                        return a;
                }
            }

            return null;
        }

        static IReadOnlyList<WParty> GetParties(MapEventSide side)
        {
            if (side == null)
                return [];

            var list = new List<WParty>(side.Parties.Count);
            foreach (var p in side.Parties)
            {
                var mp = p?.Party?.MobileParty;
                if (mp == null)
                    continue;

                var wp = WParty.Get(mp);
                if (wp != null)
                    list.Add(wp);
            }

            return list;
        }

        static IReadOnlyList<WHero> GetPartyLeaders(MapEventSide side)
        {
            if (side == null)
                return [];

            var list = new List<WHero>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var p in side.Parties)
            {
                var hero = p?.Party?.MobileParty?.LeaderHero;
                if (hero == null)
                    continue;

                var id = hero.StringId;
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                    continue;

                var wh = WHero.Get(hero);
                if (wh != null)
                    list.Add(wh);
            }

            return list;
        }

        static int GetTroopCount(MapEventSide side) => side?.TroopCount ?? 0;

        static float GetTotalStrength(MapEventSide side)
        {
            if (side == null)
                return 0f;

            float total = 0f;
            foreach (var wp in GetParties(side))
                total += wp.Strength;

            return total;
        }
    }
}
