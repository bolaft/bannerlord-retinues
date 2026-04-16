using System.Collections.Generic;
using System.Reflection;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Events.Helpers;
using Retinues.Domain.Parties.Wrappers;
using Retinues.Domain.Settlements.Wrappers;
using Retinues.Framework.Model;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Domain.Events.Models
{
    /// <summary>
    /// Live wrapper around a MapEvent.
    /// </summary>
    public sealed class MMapEvent(MapEvent @base) : MBase<MapEvent>(@base)
    {
        // Reflected BL13-only members (null on BL14+ at runtime).
        private static readonly MethodInfo _getBattleRewardsMethod = typeof(MapEvent).GetMethod(
            "GetBattleRewards",
            BindingFlags.Public | BindingFlags.Instance
        );
        private static readonly PropertyInfo _moraleChangeProperty =
            typeof(MapEventParty).GetProperty(
                "MoraleChange",
                BindingFlags.Public | BindingFlags.Instance
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Settlement                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WSettlement Settlement => WSettlement.Get(Base.MapEventSettlement);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Event Type                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public MapEvent.BattleTypes EventType => Base.EventType;

        public bool IsFieldBattle => Base.EventType == MapEvent.BattleTypes.FieldBattle;
        public bool IsSiegeBattle => Base.EventType == MapEvent.BattleTypes.Siege;
        public bool IsNavalBattle => NavalBattleHelper.IsNavalBattle(Base);
        public bool IsRaid => Base.EventType == MapEvent.BattleTypes.Raid;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Result                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsPlayerInvolved => Base.IsPlayerMapEvent;

        public BattleSideEnum PlayerSideEnum => Base.PlayerSide;

        public bool HasWinner => Base.HasWinner;

        public BattleSideEnum WinningSide => Base.WinningSide;
        public BattleSideEnum DefeatedSide => Base.DefeatedSide;

        public bool IsResolved => HasWinner && WinningSide != BattleSideEnum.None;

        public bool IsWon =>
            IsResolved
            && IsPlayerInvolved
            && PlayerSideEnum != BattleSideEnum.None
            && WinningSide == PlayerSideEnum;

        public bool IsLost =>
            IsResolved
            && IsPlayerInvolved
            && PlayerSideEnum != BattleSideEnum.None
            && WinningSide != PlayerSideEnum;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Armies                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsPlayerInArmy => Player.Party.IsInArmy;
        public bool IsEnemyInArmy =>
            (
                PlayerSideEnum == BattleSideEnum.Attacker
                && DefenderSide.LeaderParty?.IsInArmy == true
            )
            || (
                PlayerSideEnum == BattleSideEnum.Defender
                && AttackerSide.LeaderParty?.IsInArmy == true
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Rewards                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        bool rewardsComputed;

        float _renownReward = 0f;
        float _influenceReward = 0f;
        float _moraleReward = 0f;
        float _goldReward = 0f;

        public int RenownReward
        {
            get
            {
                EnsureRewards();
                return MathF.Round(_renownReward);
            }
        }

        public int InfluenceReward
        {
            get
            {
                EnsureRewards();
                return MathF.Round(_influenceReward);
            }
        }

        public int MoraleReward
        {
            get
            {
                EnsureRewards();
                return MathF.Round(_moraleReward);
            }
        }

        public int GoldReward
        {
            get
            {
                EnsureRewards();
                return MathF.Round(_goldReward);
            }
        }

        void EnsureRewards()
        {
            if (rewardsComputed)
                return;

            rewardsComputed = true;

            if (Base == null || !IsPlayerInvolved)
                return;

            try
            {
#if BL13 || BL14
                if (_getBattleRewardsMethod != null)
                {
                    var args = new object[] { Player.Party.PartyBase, 0f, 0f, 0f, 0f, null };
                    _getBattleRewardsMethod.Invoke(Base, args);
                    _renownReward = (float)args[1];
                    _influenceReward = (float)args[2];
                    _moraleReward = (float)args[3];
                    _goldReward = (float)args[4];
                }
#endif
            }
            catch
            {
                // Live computed property.
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Sides                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public SideData PlayerSide => AttackerSide.IsPlayerSide ? AttackerSide : DefenderSide;
        public SideData EnemySide => AttackerSide.IsPlayerSide ? DefenderSide : AttackerSide;

        public SideData AttackerSide => BuildSide(BattleSideEnum.Attacker);
        public SideData DefenderSide => BuildSide(BattleSideEnum.Defender);

        /// <summary>
        /// Builds the side data for the given side enum.
        /// </summary>
        SideData BuildSide(BattleSideEnum sideEnum)
        {
            if (Base == null)
                return null;

            var side = Base.GetMapEventSide(sideEnum);
            if (side == null)
                return null;

            int men = 0;
            try
            {
                men = Base.GetNumberOfInvolvedMen(sideEnum);
            }
            catch { }

            var parties = new List<PartyData>();
            if (side.Parties != null)
            {
                for (int i = 0; i < side.Parties.Count; i++)
                {
                    var p = side.Parties[i];
                    var party = p?.Party;

                    int healthyEnd = 0;
                    try
                    {
                        healthyEnd = party?.MemberRoster?.TotalHealthyCount ?? 0;
                    }
                    catch { }

                    int healthyStart = p?.HealthyManCountAtStart ?? healthyEnd;

                    parties.Add(
                        new PartyData(
                            partyId: party?.MobileParty?.StringId,
                            leaderId: party?.LeaderHero?.StringId,
                            healthyStart: healthyStart,
                            healthyEnd: healthyEnd,
                            contributionToBattle: p?.ContributionToBattle ?? 0,
                            gainedRenown: p?.GainedRenown ?? 0f,
                            gainedInfluence: p?.GainedInfluence ?? 0f,
                            plunderedGold: p?.PlunderedGold ?? 0,
                            goldLost: p?.GoldLost ?? 0
#if BL13 || BL14
                            ,
                            moraleChange: _moraleChangeProperty != null
                                ? (_moraleChangeProperty.GetValue(p) as float? ?? 0f)
                                : 0f
#endif
                        )
                    );
                }
            }

            return new SideData(
                side: sideEnum,
                isPlayerSide: sideEnum == PlayerSideEnum,
                isInArmy: side.LeaderParty?.MobileParty?.Army != null,
                leaderPartyId: side.LeaderParty?.MobileParty?.StringId,
                menInvolved: men,
                healthyTroops: side.GetTotalHealthyTroopCountOfSide(),
                healthyHeroes: side.GetTotalHealthyHeroCountOfSide(),
                strength: side.StrengthRatio,
                parties: parties
            );
        }

        /// <summary>
        /// Data about a battle side.
        /// </summary>
        public sealed class SideData(
            BattleSideEnum side,
            bool isPlayerSide,
            bool isInArmy,
            string leaderPartyId,
            int menInvolved,
            int healthyTroops,
            int healthyHeroes,
            float strength,
            IReadOnlyList<PartyData> parties
        )
        {
            public BattleSideEnum Side { get; } = side;
            public bool IsPlayerSide { get; } = isPlayerSide;
            public bool IsEnemySide { get; } = !isPlayerSide;
            public bool IsInArmy { get; } = isInArmy;

            public string LeaderPartyId { get; } = leaderPartyId;
            public WParty LeaderParty => WParty.Get(LeaderPartyId);

            public int MenInvolved { get; } = menInvolved;
            public int HealthyTroops { get; } = healthyTroops;
            public int HealthyHeroes { get; } = healthyHeroes;

            public float Strength { get; } = strength;

            public IReadOnlyList<PartyData> PartyData { get; } = parties ?? [];
            public IEnumerable<WParty> Parties
            {
                get
                {
                    foreach (var p in PartyData)
                    {
                        var party = p?.Party;
                        if (party != null)
                            yield return party;
                    }
                }
            }
        }

        /// <summary>
        /// Data about a party involved in a battle side.
        /// </summary>
        public sealed class PartyData(
            string partyId,
            string leaderId,
            int healthyStart,
            int healthyEnd,
            int contributionToBattle,
            float gainedRenown,
            float gainedInfluence,
            int plunderedGold,
            int goldLost,
            float moraleChange = 0f
        )
        {
            public string PartyId { get; } = partyId;
            public WParty Party => WParty.Get(PartyId);
            public string LeaderId { get; } = leaderId;
            public WHero Hero => WHero.Get(LeaderId);

            public int HealthyStart { get; } = healthyStart;
            public int HealthyEnd { get; } = healthyEnd;

            public int ContributionToBattle { get; } = contributionToBattle;
            public float GainedRenown { get; } = gainedRenown;
            public float GainedInfluence { get; } = gainedInfluence;
            public float MoraleChange { get; } = moraleChange;
            public int PlunderedGold { get; } = plunderedGold;
            public int GoldLost { get; } = goldLost;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Snapshot                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates a snapshot of the current map event state.
        /// </summary>
        public Snapshot TakeSnapshot() => new(this);

        /// <summary>
        /// Snapshot of a map event state at a point in time.
        /// </summary>
        public sealed class Snapshot(MMapEvent me)
        {
            /* ━━━━━━ Settlement ━━━━━━ */

            public string SettlementId { get; } = me.Settlement?.StringId;
            public WSettlement Settlement => WSettlement.Get(SettlementId);

            /* ━━━━━━━━━ Flags ━━━━━━━━ */

            public bool IsFieldBattle { get; } = me.IsFieldBattle;
            public bool IsSiegeBattle { get; } = me.IsSiegeBattle;
            public bool IsNavalBattle { get; } = me.IsNavalBattle;
            public bool IsRaid { get; } = me.IsRaid;
            public bool IsPlayerInArmy { get; } = me.IsPlayerInArmy;
            public bool IsEnemyInArmy { get; } = me.IsEnemyInArmy;

            /* ━━━━━━━━ Result ━━━━━━━━ */

            public bool IsPlayerInvolved { get; } = me.IsPlayerInvolved;
            public BattleSideEnum PlayerSideEnum { get; } = me.PlayerSideEnum;

            public bool HasWinner { get; } = me.HasWinner;
            public BattleSideEnum WinningSide { get; } = me.WinningSide;
            public BattleSideEnum DefeatedSide { get; } = me.DefeatedSide;

            public bool IsResolved { get; } = me.IsResolved;
            public bool IsWon { get; } = me.IsWon;

            public bool IsLost { get; } = me.IsLost;

            /* ━━━━━━━━ Rewards ━━━━━━━ */

            public int RenownReward { get; } = me.RenownReward;
            public int InfluenceReward { get; } = me.InfluenceReward;
            public int MoraleReward { get; } = me.MoraleReward;
            public int GoldReward { get; } = me.GoldReward;

            /* ━━━━━━━━━ Sides ━━━━━━━━ */

            public SideData PlayerSide => AttackerSide.IsPlayerSide ? AttackerSide : DefenderSide;
            public SideData EnemySide => AttackerSide.IsPlayerSide ? DefenderSide : AttackerSide;

            public SideData AttackerSide { get; } = me.AttackerSide;
            public SideData DefenderSide { get; } = me.DefenderSide;
        }
    }
}
