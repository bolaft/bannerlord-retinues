using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Objects;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class CultureWrapper
    {
        public string Name => _culture.Name.ToString();

        public string StringId => _culture.StringId.ToString();

        public CharacterWrapper RootBasic => new CharacterWrapper(_culture.BasicTroop);

        public CharacterWrapper RootElite => new CharacterWrapper(_culture.EliteBasicTroop);

        private CultureObject _culture;

        public CultureWrapper(CultureObject culture)
        {
            _culture = culture;
        }
    }
}
