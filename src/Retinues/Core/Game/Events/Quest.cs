using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Events
{
    public class Quest(QuestBase quest, bool isSuccessful)
    {
        // =========================================================================
        // Fields
        // =========================================================================

        private readonly QuestBase _quest = quest;

        // =========================================================================
        // Info
        // =========================================================================

        public bool IsSuccessful = isSuccessful;

        public Hero Giver => _quest.QuestGiver;

        public bool RefusedPayment => _quest.RewardGold == 0;
    }
}