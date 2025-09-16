using Retinues.Core.Game.Events;
using Retinues.Core.Utils;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class BoundByHonor : Doctrine
    {
        public override string Name => L.S("bound_by_honor", "Bound by Honor");
        public override string Description =>
            L.S("bound_by_honor_description", "+20% retinue morale.");
        public override int Column => 3;
        public override int Row => 1;

        public sealed class BBH_NoPayment3x : Feat
        {
            public override string Description =>
                L.S(
                    "bound_by_honor_no_payment_3x",
                    "Complete a quest for no reward three times."
                );
            public override int Target => 3;

            public override void OnQuestCompleted(Quest quest)
            {
                if (!quest.IsSuccessful)
                    return;
                if (!quest.NoPayment)
                    return;

                AdvanceProgress(1);
            }
        }

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

        public sealed class BBH_RetinueWinsTournament : Feat
        {
            public override string Description =>
                L.S(
                    "bound_by_honor_retinue_wins_tournament",
                    "Have one of your retinue win a tournament."
                );
            public override int Target => 1;

            public override void OnTournamentFinished(Tournament tournament)
            {
                if (tournament.Winner?.IsRetinue == true)
                    AdvanceProgress(1);
            }
        }
    }
}
