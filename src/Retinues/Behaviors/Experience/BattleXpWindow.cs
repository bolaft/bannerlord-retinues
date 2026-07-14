using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;

namespace Retinues.Behaviors.Experience
{
    /// <summary>
    /// Tracks whether the player is currently inside a battle (a player-involved map event).
    ///
    /// Troop combat XP is routed through <c>TroopRoster.AddXpToTroopAtIndex</c> both while the
    /// mission is live AND during battle aftermath — by which point <c>Mission.Current</c> may
    /// already be null. Gating skill-point capture on <c>Mission.Current</c> therefore dropped the
    /// aftermath portion of battle XP. This window instead spans the whole map event (start to end,
    /// aftermath included), so battle XP is captured in full while campaign-map daily training XP
    /// (which never runs inside a player map event) is cleanly distinguished from it.
    /// </summary>
    internal static class BattleXpWindow
    {
        private static readonly HashSet<MapEvent> _open = [];

        /// <summary>True while at least one player-involved map event is in progress.</summary>
        public static bool IsOpen => _open.Count > 0;

        /// <summary>Marks a map event as an open battle window.</summary>
        public static void Open(MapEvent mapEvent)
        {
            if (mapEvent != null)
                _open.Add(mapEvent);
        }

        /// <summary>Clears a map event's battle window.</summary>
        public static void Close(MapEvent mapEvent)
        {
            if (mapEvent != null)
                _open.Remove(mapEvent);
        }

        /// <summary>
        /// Drops all tracked events. Called on game load so a save taken mid-encounter can't leave a
        /// stale window open (which would misclassify later training XP as battle XP).
        /// </summary>
        public static void Reset() => _open.Clear();
    }
}
