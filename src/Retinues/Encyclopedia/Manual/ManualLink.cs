using TaleWorlds.CampaignSystem;

namespace Retinues.Encyclopedia.Manual
{
    public static class ManualLink
    {
        public static void Open() =>
            Campaign.Current?.EncyclopediaManager?.GoToLink("Concept", ManualConceptIds.Overview);
    }
}
