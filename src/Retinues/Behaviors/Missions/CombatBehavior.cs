using System.Collections.Generic;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Domain.Events.Models;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Game.Missions
{
    /// <summary>
    /// Tracks mission and map event states and keep a record of kills.
    /// </summary>
    public sealed class CombatBehavior : BaseMissionBehavior
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static CombatBehavior _instance;
        public static CombatBehavior Instance => _instance;

        private MMapEvent.Snapshot _snapshot;
        public static MMapEvent.Snapshot Snapshot => Instance?._snapshot;
        private MMapEvent _mapEvent;
        public static MMapEvent MapEvent => Instance?._mapEvent;
        private MMission _mission;

        public CombatBehavior()
        {
            _instance = this;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Kills                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Represents a kill that occurred during the mission.
        /// </summary>
        public readonly struct Kill(
            MAgent.Snapshot victim,
            MAgent.Snapshot killer,
            AgentState state,
            KillingBlow blow
        )
        {
            /* ━━━━━━━━ Agents ━━━━━━━━ */

            public readonly MAgent.Snapshot Victim = victim;
            public readonly MAgent.Snapshot Killer = killer;

            /* ━━━━━━━━━ State ━━━━━━━━ */

            public readonly AgentState State = state;

            /* ━━━━━━━━━ Blow ━━━━━━━━━ */

            public readonly bool IsMissile = blow.IsMissile;
            public readonly bool IsHeadShot = blow.VictimBodyPart == BoneBodyPartType.Head;
            public readonly int WeaponClass = blow.WeaponClass;

            /* ━━━━━━ Characters ━━━━━━ */

            public string VictimCharacterId => Victim?.CharacterId;
            public WCharacter VictimCharacter =>
                VictimCharacterId != null ? WCharacter.Get(VictimCharacterId) : null;

            public string KillerCharacterId => Killer?.CharacterId;
            public WCharacter KillerCharacter =>
                KillerCharacterId != null ? WCharacter.Get(KillerCharacterId) : null;

            /* ━━━━━━ Equipments ━━━━━━ */

            public readonly string VictimEquipmentCode = victim?.EquipmentCode;
            public MEquipment VictimEquipment =>
                MEquipment.FromCode(VictimCharacter, VictimEquipmentCode);

            public readonly string KillerEquipmentCode = killer?.EquipmentCode;
            public MEquipment KillerEquipment =>
                MEquipment.FromCode(KillerCharacter, KillerEquipmentCode);
        }

        private readonly List<Kill> _kills = [];
        public IReadOnlyList<Kill> Kills => _kills;

        public static IReadOnlyList<Kill> GetKills() => Instance?._kills;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Start                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _started;
        private bool _ended;

        /// <summary>
        /// Handles mission start logic.
        /// </summary>
        public override void AfterStart()
        {
            if (_started)
                return; // Prevent double-start.

            _started = true;
            _ended = false;

            // Clear any prior kills.
            _kills.Clear();

            // Capture current map event take a snapshot.
            var me = Player.Party?.Base.MapEvent;
            if (me != null)
            {
                _mapEvent = new MMapEvent(me);
                _snapshot = _mapEvent.TakeSnapshot();
            }

            // Capture current mission.
            _mission = Mission != null ? new MMission(Mission) : null;

            Log.Debug(
                $"Mission started. Mode='{_mission?.Mode.ToString()}'. Scene='{_mission?.SceneName}'."
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           End                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnEndMission() => End();

        public override void OnRemoveBehavior() => End();

        /// <summary>
        /// Handles mission end logic.
        /// </summary>
        private void End()
        {
            if (_ended)
                return; // Prevent double-end.

            _ended = true;
            _started = false;

            Log.Debug(
                $"Mission ended. Mode='{_mission?.Mode.ToString()}'. Scene='{_mission?.SceneName}'."
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Kill                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Handles agent removal to capture kills.
        /// </summary>
        public override void OnAgentRemoved(
            Agent victim,
            Agent killer,
            AgentState state,
            KillingBlow blow
        )
        {
            if (!IsValidKill(victim, killer, state))
                return;

            var vLive =
                victim != null ? new MAgent(victim, mission: _mission, mapEvent: _mapEvent) : null;
            var kLive =
                killer != null ? new MAgent(killer, mission: _mission, mapEvent: _mapEvent) : null;

            var v = vLive?.TakeSnapshot();
            var k = kLive?.TakeSnapshot();

            if (!IsValidSnapshot(v) || !IsValidSnapshot(k))
                return;

            var kill = new Kill(v, k, state, blow);
            _kills.Add(kill);
        }

        /// <summary>
        /// Determines if the kill data is valid.
        /// </summary>
        private bool IsValidKill(Agent victim, Agent killer, AgentState state)
        {
            return victim != null
                && killer != null
                && !string.IsNullOrEmpty(victim.Character.StringId)
                && !string.IsNullOrEmpty(killer.Character.StringId)
                && (state == AgentState.Killed || state == AgentState.Unconscious);
        }

        /// <summary>
        /// Determines if the agent snapshot is valid.
        /// </summary>
        private bool IsValidSnapshot(MAgent.Snapshot snapshot)
        {
            if (snapshot == null)
                return false;

            if (string.IsNullOrEmpty(snapshot.CharacterId))
                return false;

            return true;
        }
    }
}
