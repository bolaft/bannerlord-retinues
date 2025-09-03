using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Objects;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class CultureWrapper(CultureObject culture)
    {
        // =========================================================================
        // Base
        // =========================================================================

        private CultureObject _culture = culture;

        public CultureObject Base => _culture;

        // =========================================================================
        // Properties
        // =========================================================================

        public string Name => _culture.Name.ToString();

        public string StringId => _culture.StringId.ToString();

        public CharacterWrapper RootBasic => new(_culture.BasicTroop);

        public CharacterWrapper RootElite => new(_culture.EliteBasicTroop);
    }
}
