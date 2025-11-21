using System.Linq;
using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class PragmaticScavengers : Doctrine
    {
        public override TextObject Name => L.T("pragmatic_scavengers", "Pragmatic Scavengers");
        public override TextObject Description =>
            L.T(
                "pragmatic_scavengers_description",
                "Can unlock items from allied party casualties."
            );
        public override int Column => 0;
        public override int Row => 2;
        public override bool IsDisabled => !Config.UnlockFromKills || Config.AllEquipmentUnlocked;
        public override TextObject DisabledMessage =>
            Config.AllEquipmentUnlocked
                ? L.T(
                    "pragmatic_scavengers_disabled_message_all_equipment",
                    "Disabled: all equipment already unlocked by config."
                )
                : L.T(
                    "pragmatic_scavengers_disabled_message_unlocks_from_kills",
                    "Disabled: unlocks from kills disabled by config."
                );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class PS_Allies100Casualties : Feat
        {
            public override TextObject Description =>
                L.T(
                    "pragmatic_scavengers_defense_allies_100",
                    "Win a battle in which allies suffer over 100 casualties."
                );
            public override int Target => 100;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return; // Must win

                if (battle.AllyTroopCount == 0)
                    return; // No allies, no progress

                int allyCasualties = battle.Kills.Where(k => k.VictimIsAllyTroop).Count();

                if (allyCasualties > Progress)
                    SetProgress(allyCasualties);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class PS_AllyArmyWin3 : Feat
        {
            public override TextObject Description =>
                L.T(
                    "pragmatic_scavengers_army_win_allies_50",
                    "Win three battles in a row while part of an allied lord's army."
                );
            public override int Target => 3;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost || !battle.PlayerIsInArmy || Player.IsArmyLeader)
                    SetProgress(0);
                else
                    AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class PS_RescueLord : Feat
        {
            public override TextObject Description =>
                L.T(
                    "pragmatic_scavengers_rescue_lord",
                    "Rescue a defeated lord from a enemy party's prisoner train."
                );
            public override int Target => 1;

            private static bool LordInCaptivity = false;

            public override void OnBattleStart(Battle battle)
            {
                LordInCaptivity = false;

                foreach (var p in battle.EnemyPrisoners)
                {
                    if (p.IsHero)
                    {
                        LordInCaptivity = true;
                        return;
                    }
                }
            }

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (!LordInCaptivity)
                    return;
                AdvanceProgress(1);
            }
        }
    }
}
