using Retinues.Core.Mods.Shokuho;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Mods
{
    [SafeClass]
    public static class ModCompatibility
    {
        public static void AddBehaviors(CampaignGameStarter cs)
        {
            if (ShokuhoDetect.IsShokuhoCampaign())
            {
                Log.Debug("Shokuho detected, using ShokuhoVolunteerSwapBehavior.");
                cs.AddBehavior(new ShokuhoVolunteerSwapBehavior());
            }
        }
    }
}
