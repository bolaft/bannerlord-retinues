using System.Collections.Generic;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Game.Events
{
    [SafeClass]
    public class Tournament(
        WSettlement town = null,
        WCharacter winner = null,
        List<WCharacter> participants = null
    )
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WSettlement Town = town;
        public WCharacter Winner = winner;
        public List<WCharacter> Participants = participants ?? [];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Updates                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void UpdateOnFinish(WCharacter winner, List<WCharacter> participants)
        {
            Winner = winner;
            Participants = participants ?? Participants;
        }
    }
}
