using Retinues.Core.Game.Events;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class BoundByHonor : Doctrine
    {
        public override string Name => L.S("bound_by_honor", "Bound by Honor");
        public override string Description => L.S("bound_by_honor_description", "+20% retinue morale.");
        public override int Column => 3;
        public override int Row => 1;

        public sealed class BBH_RefusePayment3x : Feat
        {
            public override string Description => L.S("bound_by_honor_refuse_payment_3x", "Refuse payment for mercenary work three times.");
            public override int Target => 3;

            public override void OnQuestCompleted(Quest quest)
            {
                if (!quest.IsSuccessful) return;
                if (!quest.RefusedPayment) return;

                AdvanceProgress(1);
            }
        }

        public sealed class BBH_RetinueOnlyMorale80For30Days : Feat
        {
            public override string Description => L.S("bound_by_honor_retinue_only_morale_80_for_30_days", "Maintain a retinue-only party's morale above 80 for 30 days.");
            public override int Target => 30;

            public override void OnDailyTick()
            {
                if (Player.Party.Morale > 80 && Player.Party.MemberRoster.RetinueRatio > 0.99f)
                    AdvanceProgress(1);
                else
                    SetProgress(0);  // Reset progress if morale drops below 80
            }
        }

        public sealed class BBH_RetinueWinsTournament : Feat
        {
            public override string Description => L.S("bound_by_honor_retinue_wins_tournament", "Have one of your retinue win a tournament.");
            public override int Target => 1;

            public override void OnTournamentFinished(Tournament tournament)
            {
                if (tournament.Winner?.IsRetinue == true)
                    AdvanceProgress(1);
            }
        }
    }
}
