using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class AncestralHeritage : Doctrine
    {
        public override TextObject Name => L.T("ancestral_heritage", "Ancestral Heritage");
        public override TextObject Description =>
            L.T(
                "ancestral_heritage_description",
                "Unlocks all items of clan and kingdom cultures."
            );
        public override int Column => 0;
        public override int Row => 3;
        public override bool IsDisabled => Config.UnlockFromCulture || Config.AllEquipmentUnlocked;
        public override TextObject DisabledMessage =>
            Config.AllEquipmentUnlocked
                ? L.T(
                    "ancestral_heritage_disabled_message_all_equipment",
                    "Disabled: all equipment already unlocked by config."
                )
                : L.T(
                    "ancestral_heritage_disabled_message_culture_items",
                    "Disabled: culture items already unlocked by config."
                );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class AH_150OutnumberedOwnCulture : Feat
        {
            public override TextObject Description =>
                L.T(
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

                var culture = Player.Culture;

                foreach (var e in playerRoster.Elements)
                {
                    if (e.Troop.IsHero)
                        continue;
                    if (!e.Troop.IsCustom)
                        return;
                    if (e.Troop.Culture != culture)
                        return;
                }

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class AH_TournamentOwnCultureTown : Feat
        {
            public override TextObject Description =>
                L.T(
                    "ancestral_heritage_tournament_own_culture_town",
                    "Win a tournament in a town of your own culture."
                );
            public override int Target => 1;

            public override void OnTournamentFinished(Tournament tournament)
            {
                if (tournament.Winner != Player.Character)
                    return;
                if (tournament.Town?.Culture != Player.Culture)
                    return;

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class AH_CaptureOwnCultureFief : Feat
        {
            public override TextObject Description =>
                L.T(
                    "ancestral_heritage_capture_own_culture_fief",
                    "Capture a fief of your own culture from an enemy kingdom."
                );
            public override int Target => 1;

            public override void OnSettlementOwnerChanged(SettlementOwnerChange change)
            {
                if (!change.WasCaptured)
                    return;
                if (change.NewOwner != Player.Character)
                    return;
                if (change.Settlement?.Culture != Player.Culture)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
