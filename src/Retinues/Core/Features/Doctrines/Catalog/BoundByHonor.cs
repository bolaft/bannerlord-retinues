using System.Linq;
using Retinues.Core.Features.Doctrines.Model;
using Retinues.Core.Game;
using Retinues.Core.Game.Events;

namespace Retinues.Core.Features.Doctrines.Catalog
{
    public sealed class BoundByHonor : Doctrine
    {
        public override string Name => L.S("bound_by_honor", "Bound by Honor");
        public override string Description =>
            L.S("bound_by_honor_description", "+20% retinue morale.");
        public override int Column => 3;
        public override int Row => 1;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class BBH_ProtectVillagersOrCaravans : Feat
        {
            public override string Description =>
                L.S("bound_by_honor_protect_villagers_or_caravans", "Save 3 caravans or villager parties from enemy attacks.");
            public override int Target => 3;

            public override void OnBattleEnd(Battle battle)
            {
                if (!battle.IsWon)
                    return;
                
                var rescuedParties = battle.AllyParties.Count(p => p.IsCaravan || p.IsVillager);

                AdvanceProgress(rescuedParties);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class BBH_RetinueOnlyMorale90For15Days : Feat
        {
            public override string Description =>
                L.S(
                    "bound_by_honor_retinue_only_morale_90_for_15_days",
                    "Maintain a retinue-only party's morale above 90 for 15 days."
                );
            public override int Target => 15;

            public override void OnDailyTick()
            {
                if (Player.Party.Morale > 90 && Player.Party.MemberRoster.RetinueRatio > 0.99f)
                    AdvanceProgress(1);
                else
                    SetProgress(0); // Reset progress if morale drops below 90
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class BBH_Defeat10Bandits : Feat
        {
            public override string Description =>
                L.S(
                    "bound_by_honor_defeat_10_bandits",
                    "Get rid of 10 bandit parties."
                );
            public override int Target => 10;

            public override void OnBattleEnd(Battle battle)
            {
                if (!battle.IsWon)
                    return;

                var defeatedBanditParties = battle.EnemyParties.Count(p => p.IsBandit);

                AdvanceProgress(defeatedBanditParties);
            }
        }
    }
}
