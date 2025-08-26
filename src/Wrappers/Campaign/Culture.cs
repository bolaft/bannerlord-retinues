using TaleWorlds.CampaignSystem;
using CustomClanTroops.Wrappers.Objects;

namespace CustomClanTroops.Wrappers.Campaign
{
    public class CultureWrapper
    {
        public string Name { get; set; }

        public string StringId { get; set; }

        public CharacterWrapper RootBasic { get; set; }
        public CharacterWrapper RootElite { get; set; }

        private CultureObject _culture;

        public CultureObject BaseCulture => _culture;

        public CultureWrapper(CultureObject culture)
        {
            _culture = culture;
            Name = _culture.Name.ToString();
            StringId = _culture.StringId.ToString();

            RootBasic = new CharacterWrapper(culture.BasicTroop);
            RootElite = new CharacterWrapper(culture.EliteBasicTroop);
        }
    }
}
