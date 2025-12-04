using Retinues.Utilities;
using TaleWorlds.MountAndBlade;

namespace Retinues.Behaviors
{
    /// <summary>
    /// Base class for Retinues mission behaviors.
    /// Provides IsEnabled flag and logging helpers.
    /// </summary>
    public abstract class BaseMissionBehavior : MissionBehavior
    {
        /// <summary>
        /// The type of mission behavior.
        /// </summary>
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        /// <summary>
        /// Whether this mission behavior should be active.
        /// Recommended pattern in children:
        ///   public static bool Enabled => Config.EnableMyMissionBehavior;
        ///   public override bool IsEnabled => Enabled;
        /// </summary>
        public virtual bool IsEnabled => true;
    }
}
