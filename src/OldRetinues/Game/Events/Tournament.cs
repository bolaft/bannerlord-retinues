using System.Collections.Generic;
using Retinues.Game.Wrappers;
using Retinues.Utils;

namespace OldRetinues.Game.Events
{
    /// <summary>
    /// Tournament event wrapper, provides info about town, winner, and participants.
    /// </summary>
    [SafeClass]
    public class Tournament(WSettlement town, WCharacter winner, List<WCharacter> participants)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WSettlement Town = town;
        public WCharacter Winner = winner;
        public List<WCharacter> Participants = participants ?? [];
    }
}
