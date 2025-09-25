using System.Collections.Generic;
using Retinues.Core.Game.Wrappers;
using TaleWorlds.CampaignSystem.Settlements;

namespace Retinues.Core.Game.Events
{
    public class Tournament(
        Town town,
        WCharacter winner = null,
        List<WCharacter> participants = null
    )
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public Town Town = town;
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
