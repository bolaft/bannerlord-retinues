using Retinues.Core.Game.Events;

namespace Retinues.Core.Game.Features.Doctrines.Catalog
{
    public sealed class BattlefieldTithes : Doctrine
    {
        public override string Name => "Battlefield Tithes";
        public override string Description => "Can unlock items from allied party kills.";
        public override int Column => 0;
        public override int Row => 1;

        public sealed class BT_QuestForAlliedLord : Feat
        {
            public override string Description => "Complete a quest for an allied lord.";
            public override int Target => 1;

            public override void OnQuestCompleted(Quest quest)
            {
                if (!quest.IsSuccessful) return;
                if (quest.Giver.MapFaction.StringId != Player.MapFaction.StringId) return;

                AdvanceProgress(1);
            }
        }

        public sealed class BT_SaveAlliedLord : Feat
        {
            public override string Description => "Save an allied lord from certain defeat.";
            public override int Target => 1;

            private static bool IsCandidate = false;

            public override void OnBattleStart(Battle battle)
            {
                IsCandidate = false;

                if (battle.AllyTroopCount == 0) return;
                if (battle.EnemyTroopCount < 2 * battle.AllyTroopCount) return;

                IsCandidate = true;
            }

            public override void OnBattleEnd(Battle battle)
            {
                if (!IsCandidate) return;
                if (battle.IsLost) return;

                AdvanceProgress(1);
            }
        }

        public sealed class BT_TurnTideAlliedArmyBattle : Feat
        {
            public override string Description => "Turn the tide of a battle involving an allied army.";
            public override int Target => 1;

            private static bool IsCandidate = false;

            public override void OnBattleStart(Battle battle)
            {
                IsCandidate = false;

                if (battle.AllyTroopCount == 0) return;
                if (battle.EnemyTroopCount < 2 * battle.AllyTroopCount) return;
                if (!battle.AllyIsInArmy) return;

                IsCandidate = true;
            }

            public override void OnBattleEnd(Battle battle)
            {
                if (!IsCandidate) return;
                if (battle.IsLost) return;

                AdvanceProgress(1);
            }
        }
    }
}
