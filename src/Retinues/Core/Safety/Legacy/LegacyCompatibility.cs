using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Safety.Legacy
{
    [SafeClass]
    public static class LegacyCompatibility
    {
        public static void AddBehaviors(CampaignGameStarter cs)
        {
            cs.AddBehavior(new TroopSaveBehavior());
            cs.AddBehavior(new ItemSaveBehavior());
        }
    }
}
