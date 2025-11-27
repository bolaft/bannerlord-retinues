using Retinues.Configuration;
using Retinues.Doctrines.Model;
using Retinues.Game;
using Retinues.Game.Events;
using Retinues.Utils;
using TaleWorlds.Localization;

namespace Retinues.Doctrines.Catalog
{
    public sealed class BattlefieldTithes : Doctrine
    {
        public override TextObject Name => L.T("battlefield_tithes", "Battlefield Tithes");
        public override TextObject Description =>
            L.T("battlefield_tithes_description", "Unlock items from alliy kills.");
        public override int Column => 0;
        public override int Row => 1;
        public override bool IsDisabled =>
            !Config.UnlockItemsFromKills || Config.AllEquipmentUnlocked;
        public override TextObject DisabledMessage =>
            Config.AllEquipmentUnlocked
                ? L.T(
                    "battlefield_tithes_disabled_message_all_equipment",
                    "Disabled: all equipment already unlocked by config."
                )
                : L.T(
                    "battlefield_tithes_disabled_message_unlocks_from_kills",
                    "Disabled: unlocks from kills disabled by config."
                );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class BT_QuestForAlliedLord : Feat
        {
            public override TextObject Description =>
                L.T(
                    "battlefield_tithes_quest_for_allied_lord",
                    "Complete 5 quests for allied lords."
                );
            public override int Target => 5;

            public override void OnQuestCompleted(Quest quest)
            {
                if (!quest.IsSuccessful)
                    return;
                if (quest.Giver?.Hero.MapFaction.StringId != Player.MapFaction.StringId)
                    return;
                if (!quest.Giver?.IsPartyLeader ?? true)
                    return;

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class BT_LeadArmyVictory : Feat
        {
            public override TextObject Description =>
                L.T(
                    "battlefield_tithes_lead_army_victory",
                    "Lead an army of mostly allied troops to victory against an enemy army."
                );
            public override int Target => 1;

            public override void OnBattleEnd(Battle battle)
            {
                if (battle.IsLost)
                    return;
                if (!Player.IsArmyLeader)
                    return;
                if (battle.AllyTroopCount < battle.PlayerTroopCount)
                    return;
                if (!battle.EnemyIsInArmy)
                    return;

                AdvanceProgress(1);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public sealed class BT_TurnTideAlliedArmyBattle : Feat
        {
            public override TextObject Description =>
                L.T(
                    "battlefield_tithes_turn_tide_allied_army_battle",
                    "Turn the tide of a battle involving an allied army."
                );
            public override int Target => 1;

            private static bool IsCandidate = false;

            public override void OnBattleStart(Battle battle)
            {
                IsCandidate = false;

                if (battle.AllyTroopCount == 0)
                    return;
                if (battle.EnemyTroopCount < 1.25 * battle.AllyTroopCount)
                    return;
                if (!battle.AllyIsInArmy)
                    return;

                IsCandidate = true;
            }

            public override void OnBattleEnd(Battle battle)
            {
                if (!IsCandidate)
                    return;
                if (battle.IsLost)
                    return;

                AdvanceProgress(1);
            }
        }
    }
}
