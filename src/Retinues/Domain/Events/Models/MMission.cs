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
        public readonly struct Kill(
            MAgent victim,
            MAgent killer,
            AgentState state,
            KillingBlow blow
        )
        {
            /* ━━━━━━ Fields (readonly for fast init) ━━━━━━ */

            public readonly string VictimCharacterId = victim?.Character?.StringId;
            public readonly string KillerCharacterId = killer?.Character?.StringId;

            public readonly BattleSideEnum VictimSide = victim?.SideEnum ?? BattleSideEnum.None;
            public readonly BattleSideEnum KillerSide = killer?.SideEnum ?? BattleSideEnum.None;

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
