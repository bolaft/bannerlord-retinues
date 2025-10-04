using Retinues.Core.Game.Wrappers.Base;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Wrappers
{
    [SafeClass(SwallowByDefault = false)]
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

        public WCulture Culture => _hero?.Culture == null ? null : new(_hero.Culture);

        public override WFaction Clan => _hero?.Clan == null ? null : new(_hero.Clan);

        public override WFaction Kingdom =>
            _hero?.Clan?.Kingdom == null ? null : new(_hero.Clan.Kingdom);

        public bool IsPartyLeader => _hero?.IsPartyLeader ?? false;
    }
}
