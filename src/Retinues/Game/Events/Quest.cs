using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Game.Events
{
    /// <summary>
    /// Quest event wrapper, provides helpers for quest status, giver, and payment logic.
    /// </summary>
    [SafeClass]
    public class Quest(QuestBase quest, bool isSuccessful)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly QuestBase _quest = quest;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Info                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool IsSuccessful = isSuccessful;

        public WHero Giver => _quest?.QuestGiver != null ? new WHero(_quest.QuestGiver) : null;

        public bool NoPayment => _quest?.RewardGold == 0;
    }
}
