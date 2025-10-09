using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for CultureObject, exposing troop roots and militia for custom logic.
    /// </summary>
    [SafeClass(SwallowByDefault = false)]
    public class WCulture(CultureObject culture) : StringIdentifier
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly CultureObject _culture = culture;

        public CultureObject Base => _culture;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => _culture?.Name.ToString();

        public override string StringId => _culture?.StringId ?? Name; // Some cultures have no StringId?

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Troops                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the basic root troop for this culture.
        /// </summary>
        public WCharacter RootBasic =>
            _culture?.BasicTroop != null ? new(_culture.BasicTroop) : null;

        /// <summary>
        /// Gets the elite root troop for this culture.
        /// </summary>
        public WCharacter RootElite =>
            _culture?.EliteBasicTroop != null ? new(_culture.EliteBasicTroop) : null;

        /// <summary>
        /// Gets the melee militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaMelee =>
            _culture?.MeleeMilitiaTroop != null ? new(_culture.MeleeMilitiaTroop) : null;

        /// <summary>
        /// Gets the elite melee militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaMeleeElite =>
            _culture?.MeleeEliteMilitiaTroop != null ? new(_culture.MeleeEliteMilitiaTroop) : null;

        /// <summary>
        /// Gets the ranged militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaRanged =>
            _culture?.RangedMilitiaTroop != null ? new(_culture.RangedMilitiaTroop) : null;

        /// <summary>
        /// Gets the elite ranged militia troop for this culture.
        /// </summary>
        public WCharacter MilitiaRangedElite =>
            _culture?.RangedEliteMilitiaTroop != null
                ? new(_culture.RangedEliteMilitiaTroop)
                : null;
    }
}
