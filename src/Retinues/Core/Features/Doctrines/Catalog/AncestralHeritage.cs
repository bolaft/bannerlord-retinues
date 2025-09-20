using Retinues.Core.Features.Doctrines.Model;
using Retinues.Core.Game;
using Retinues.Core.Game.Events;
using Retinues.Core.Game.Wrappers;

namespace Retinues.Core.Features.Doctrines.Catalog
{
    public sealed class AncestralHeritage : Doctrine
    {
        public override string Name => L.S("ancestral_heritage", "Ancestral Heritage");
        public override string Description =>
            L.S(
                "ancestral_heritage_description",
                "Unlocks all items of clan and kingdom cultures."
            );
        public override int Column => 0;
        public override int Row => 3;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class AH_150OutnumberedOwnCulture : Feat
        {
            public override string Description =>
                L.S(
                    "ancestral_heritage_150_outnumbered_own_culture",
                    "Win a 150+ battle while outnumbered and fielding only custom troops of your own culture."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (battle.FriendlyTroopCount >= battle.EnemyTroopCount)
                    return;
                if (battle.TotalTroopCount < 150)
                    return;

                WRoster playerRoster = Player.Party.MemberRoster;

                if (playerRoster.CustomRatio < 0.99f)
                    return;
                var culture = Player.Culture;

                foreach (var e in playerRoster.Elements)
                    if (e.Troop.Culture.StringId != culture.StringId)
                        return;

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class AH_TournamentOwnCultureTown : Feat
        {
            public override string Description =>
                L.S(
                    "ancestral_heritage_tournament_own_culture_town",
                    "Win a tournament in a town of your own culture."
                );
            public override int Target => 1;

            public override void OnTournamentFinished(Tournament tournament)
            {
                if (tournament.Winner?.StringId != Player.Character.StringId)
                    return;
                if (tournament.Town?.Culture.StringId != Player.Culture.StringId)
                    return;

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class AH_CaptureOwnCultureFief : Feat
        {
            public override string Description =>
                L.S(
                    "ancestral_heritage_capture_own_culture_fief",
                    "Capture a fief of your own culture from an enemy kingdom."
                );
            public override int Target => 1;

            public override void OnSettlementOwnerChanged(SettlementOwnerChange change)
            {
                if (!change.WasCaptured)
                    return;
                if (change.NewOwner?.StringId != Player.Character.StringId)
                    return;
                if (change.Settlement?.Culture.StringId != Player.Culture.StringId)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
