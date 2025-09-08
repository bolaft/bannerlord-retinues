using TaleWorlds.CampaignSystem;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Wrappers
{
    public class WCulture(CultureObject culture) : StringIdentifier
    {
        // =========================================================================
        // Base
        // =========================================================================

        private readonly CultureObject _culture = culture;

        public object Base => _culture;

        // =========================================================================
        // Properties
        // =========================================================================

        public string Name => _culture.Name.ToString();

        public override string StringId => _culture.StringId.ToString();

        // =========================================================================
        // Troops
        // =========================================================================

        public WCharacter RootBasic => new(_culture.BasicTroop, null);
        public WCharacter RootElite => new(_culture.EliteBasicTroop, null);
    }
}
