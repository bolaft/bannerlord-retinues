using Retinues.Game.Wrappers.Base;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Wrappers
{
    /// <summary>
    /// Wrapper for Hero, exposes culture, clan, kingdom, and party leader status for custom logic.
    /// </summary>
    [SafeClass]
    public class WHero(Hero hero) : FactionObject
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Base                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Hero _hero = hero;

        public Hero Base => _hero;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Properties                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Name => _hero?.Name?.ToString();

        public override string StringId => _hero?.StringId;

        /// <summary>
        /// Gets the hero's culture as a WCulture wrapper, or null if missing.
        /// </summary>
        public WCulture Culture => _hero?.Culture == null ? null : new(_hero.Culture);

        /// <summary>
        /// Gets the hero's clan as a WFaction wrapper, or null if missing.
        /// </summary>
        public override WFaction Clan => _hero?.Clan == null ? null : new(_hero.Clan);

        /// <summary>
        /// Gets the hero's kingdom as a WFaction wrapper, or null if missing.
        /// </summary>
        public override WFaction Kingdom =>
            _hero?.Clan?.Kingdom == null ? null : new(_hero.Clan.Kingdom);

        public bool IsPartyLeader => _hero?.IsPartyLeader ?? false;
    }
}
