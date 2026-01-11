using Retinues.Configuration;
using Retinues.Framework.Behaviors;
using Retinues.Framework.Runtime;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    /// <summary>
    /// Base class for concrete feat behaviors that listen to campaign events and award feat progress.
    /// </summary>
    [SafeClass(IncludeDerived = true)]
    public abstract class FeatCampaignBehavior : BaseCampaignBehavior
    {
        public override bool IsEnabled =>
            Settings.EnableDoctrines && Settings.EnableFeatRequirements;

        protected abstract string FeatId { get; }

        /// <summary>
        /// Adds feat progress, completing the feat if it reaches the target.
        /// </summary>
        protected void AddProgress(int amount, TextObject source = null)
        {
            FeatsAPI.TryAddProgress(FeatId, amount, source);
        }

        /// <summary>
        /// Completes the feat immediately.
        /// </summary>
        protected void Complete(TextObject source = null)
        {
            FeatsAPI.TryComplete(FeatId, source);
        }

        /// <summary>
        /// Resets the feat progress and completion flag.
        /// </summary>
        protected void Reset()
        {
            FeatsAPI.TryReset(FeatId);
        }
    }
}
