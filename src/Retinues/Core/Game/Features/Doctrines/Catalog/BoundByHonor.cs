using Retinues.Core.Game.Events;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class BoundByHonor : Doctrine
    {
        public override string Name => "Bound by Honor";
        public override string Description => "+20% retinue morale.";
        public override int Column => 3;
        public override int Row => 1;

        public sealed class BBH_RefusePayment3x : Feat
        {
            public override string Description => "Refuse payment for mercenary work three times.";
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
            public override string Description => "Maintain a retinue-only party's morale above 80 for 30 days.";
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
            public override string Description => "Have one of your retinue win a tournament.";
            public override int Target => 1;

            public override void OnTournamentFinished(Tournament tournament)
            {
                if (tournament.Winner?.IsRetinue == true)
                    AdvanceProgress(1);
            }
        }
    }
}
