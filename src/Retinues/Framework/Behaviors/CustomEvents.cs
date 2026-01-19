using Retinues.Behaviors.Doctrines.Definitions;
using TaleWorlds.CampaignSystem;

namespace Retinues.Framework.Behaviors
{
    /// <summary>
    /// Custom campaign events for Retinues, modeled after CampaignEvents.
    /// </summary>
    public static class CustomEvents
    {
        /// <summary>
        /// Fired when a doctrine is acquired (false -> true transition).
        /// </summary>
        public static MbEvent<Doctrine> DoctrineAcquiredEvent { get; } = new();

        /// <summary>
        /// Fire the DoctrineAcquiredEvent.
        /// </summary>
        public static void FireDoctrineAcquired(Doctrine doctrine)
        {
            if (doctrine == null)
                return;

            DoctrineAcquiredEvent.Invoke(doctrine);
        }
    }
}
