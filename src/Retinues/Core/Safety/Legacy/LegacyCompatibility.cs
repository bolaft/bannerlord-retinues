using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Safety.Legacy
{
    [SafeClass]
    public static class LegacyCompatibility
    {
        public static void AddBehaviors(CampaignGameStarter cs)
        {
            cs.AddBehavior(new Behaviors.TroopSaveBehavior());
            cs.AddBehavior(new Behaviors.ItemSaveBehavior());
            cs.AddBehavior(new Behaviors.UnlocksBehavior());
        }
    }
}
