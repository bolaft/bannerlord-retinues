using System.Collections.Generic;
using Retinues.Domain.Characters.Models;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Models;
using Retinues.Framework.Model;
using Retinues.Framework.Runtime;
using Retinues.Game;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Domain.Events.Models
{
    /// <summary>
    /// Wrapper for Mission.
    /// </summary>
    public sealed class MMission(Mission @base) : MBase<Mission>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Current                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static MMission Current { get; private set; }

        internal static void SetCurrent(Mission mission)
        {
            Current = mission != null ? new MMission(mission) : null;
        }

        [StaticClearAction]
        public static void ClearCurrent() => Current = null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Scene                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string SceneName => Base.SceneName;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsArena => Base.CombatType == Mission.MissionCombatType.ArenaCombat;
        public bool IsBattle => Base.CombatType == Mission.MissionCombatType.Combat;
        public bool IsNotCombat => Base.CombatType == Mission.MissionCombatType.NoCombat;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Kills                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ref-free kill snapshot captured from OnAgentRemoved.
        /// Implemented as a readonly struct to avoid allocations.
        /// </summary>
        public readonly struct Kill(
            MAgent victim,
            MAgent killer,
            AgentState state,
            KillingBlow blow,
            MapEventSide victimSide,
            MapEventSide killerSide,
            MMapEvent mapEvent
        )
        {
            /* ━━━━━━ Fields (readonly for fast init) ━━━━━━ */

            public readonly string VictimCharacterId = victim?.Character?.StringId;
            public readonly string KillerCharacterId = killer?.Character?.StringId;
            public WCharacter Victim => WCharacter.Get(VictimCharacterId);
            public WCharacter Killer => WCharacter.Get(KillerCharacterId);

            public readonly bool VictimIsPlayer => Victim == Player.Hero.Character;
            public readonly bool KillerIsPlayer => Killer == Player.Hero.Character;

            public readonly bool VictimIsPlayerSide = victimSide == mapEvent.PlayerSide;
            public readonly bool KillerIsPlayerSide = killerSide == mapEvent.PlayerSide;

            public readonly bool VictimIsEnemyTroop = victimSide != mapEvent.PlayerSide;
            public readonly bool VictimIsPlayerTroop =
                victimSide == mapEvent.PlayerSide && victim.Party.IsMainParty;
            public readonly bool VictimIsAllyTroop =
                victimSide == mapEvent.PlayerSide && !victim.Party.IsMainParty;

            public readonly bool KillerIsEnemyTroop = killerSide != mapEvent.PlayerSide;
            public readonly bool KillerIsPlayerTroop =
                killerSide == mapEvent.PlayerSide && killer.Party.IsMainParty;
            public readonly bool KillerIsAllyTroop =
                killerSide == mapEvent.PlayerSide && !killer.Party.IsMainParty;

            public readonly AgentState State = state;

            public readonly DamageTypes DamageType = blow.DamageType;
            public readonly AgentAttackType AttackType = blow.AttackType;
            public readonly bool IsMissile = blow.IsMissile;

            public readonly BoneBodyPartType VictimBodyPart = blow.VictimBodyPart;
            public readonly bool IsHeadShot = blow.VictimBodyPart == BoneBodyPartType.Head;

            public readonly int InflictedDamage = blow.InflictedDamage;

            public readonly int WeaponClass = blow.WeaponClass;
            public readonly WeaponFlags WeaponFlags = blow.WeaponRecordWeaponFlags;
            public readonly int WeaponItemKind = blow.WeaponItemKind;

            public readonly string VictimEquipmentCode = victim?.Equipment?.Code;
            public readonly string KillerEquipmentCode = killer?.Equipment?.Code;

            public MEquipment KillerEquipment => MEquipment.FromCode(Killer, KillerEquipmentCode);
            public MEquipment VictimEquipment => MEquipment.FromCode(Victim, VictimEquipmentCode);

            public static bool IsValid(MAgent victim, MAgent killer, AgentState state)
            {
                return victim != null
                    && killer != null
                    && !string.IsNullOrEmpty(victim.Character?.StringId)
                    && !string.IsNullOrEmpty(killer.Character?.StringId)
                    && (state == AgentState.Killed || state == AgentState.Unconscious);
            }
        }

        readonly List<Kill> _kills = [];

        public IReadOnlyList<Kill> Kills => _kills;

        internal void AddKill(in Kill kill)
        {
            _kills.Add(kill);
        }

        internal void ClearKills()
        {
            if (_kills.Count > 0)
                _kills.Clear();
        }
    }
}
