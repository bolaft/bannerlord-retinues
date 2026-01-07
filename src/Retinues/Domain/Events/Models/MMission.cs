using Retinues.Framework.Model;
using Retinues.Framework.Runtime;
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

        [StaticClear]
        public static MMission Current { get; private set; }

        internal static void SetCurrent(Mission mission)
        {
            Current = mission != null ? new MMission(mission) : null;
        }

        internal static void ClearCurrentIf(Mission mission)
        {
            if (Current == null)
                return;

            if (mission != null && !ReferenceEquals(Current.Base, mission))
                return;

            Current = null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Scene                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string SceneName => Base.SceneName;
    }
}
