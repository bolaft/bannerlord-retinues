using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Safety.Legacy
{
    /// <summary>
    /// Registers legacy campaign behaviors for save compatibility with older Retinues versions.
    /// </summary>
    [SafeClass]
    public static class LegacyCompatibility
    {
        /// <summary>
        /// Adds legacy save behaviors to the campaign starter.
        /// </summary>
        public static void AddBehaviors(CampaignGameStarter cs)
        {
            cs.AddBehavior(new Behaviors.TroopSaveBehavior());
            cs.AddBehavior(new Behaviors.ItemSaveBehavior());
            cs.AddBehavior(new Behaviors.UnlocksBehavior());
        }
    }
}
