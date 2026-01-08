using System.Collections.Generic;
using Retinues.Domain.Characters.Models;
using Retinues.Framework.Model;
using Retinues.Framework.Runtime;
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
        //                         Kills                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Ref-free kill snapshot captured from OnAgentRemoved.
        /// Implemented as a readonly struct to avoid allocations.
        /// </summary>
        public readonly struct Kill
        {
            /* ━━━━━━ Fields (readonly for fast init) ━━━━━━ */

            public readonly string VictimCharacterId;
            public readonly string KillerCharacterId;

            public readonly BattleSideEnum VictimSide;
            public readonly BattleSideEnum KillerSide;

            public readonly AgentState State;

            public readonly DamageTypes DamageType;
            public readonly AgentAttackType AttackType;
            public readonly bool IsMissile;

            public readonly BoneBodyPartType VictimBodyPart;
            public readonly bool IsHeadShot;

            public readonly int InflictedDamage;

            public readonly int WeaponClass;
            public readonly WeaponFlags WeaponFlags;
            public readonly int WeaponItemKind;

            public readonly string VictimEquipmentCode;

            public Kill(MAgent victim, MAgent killer, AgentState state, KillingBlow blow)
            {
                VictimCharacterId = victim?.Character?.StringId;
                KillerCharacterId = killer?.Character?.StringId;

                VictimSide = victim?.SideEnum ?? BattleSideEnum.None;
                KillerSide = killer?.SideEnum ?? BattleSideEnum.None;

                State = state;

                DamageType = blow.DamageType;
                AttackType = blow.AttackType;
                IsMissile = blow.IsMissile;

                VictimBodyPart = blow.VictimBodyPart;
                IsHeadShot = blow.VictimBodyPart == BoneBodyPartType.Head;

                InflictedDamage = blow.InflictedDamage;

                WeaponClass = blow.WeaponClass;
                WeaponFlags = blow.WeaponRecordWeaponFlags;
                WeaponItemKind = blow.WeaponItemKind;

                VictimEquipmentCode = victim?.Equipment?.Code;
            }

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
