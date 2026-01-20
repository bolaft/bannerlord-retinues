using Retinues.Framework.Model;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Domain.Events.Models
{
    /// <summary>
    /// Live wrapper around a Mission.
    /// </summary>
    public sealed class MMission(Mission @base) : MBase<Mission>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Scene                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string SceneName => Base.SceneName;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Mode                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public MissionMode Mode => Base.Mode;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Flags                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsArena => Base.CombatType == Mission.MissionCombatType.ArenaCombat;
        public bool IsBattle => Base.CombatType == Mission.MissionCombatType.Combat;
        public bool IsNotCombat => Base.CombatType == Mission.MissionCombatType.NoCombat;
    }
}
