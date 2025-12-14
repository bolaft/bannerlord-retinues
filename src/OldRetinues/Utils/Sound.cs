using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace OldRetinues.Utils
{
    /// <summary>
    /// Centralised helpers for playing UI sound effects.
    /// </summary>
    [SafeClass]
    public static class Sound
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Constants                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public const string QuestFinished = "event:/ui/notification/quest_finished";
        public const string ReignDecision = "event:/ui/reign/decision";
        public const string TraitChange = "event:/ui/notification/trait_change";
        public const string Education = "event:/ui/notification/education";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Public Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Generic 2D sound event wrapper.
        /// </summary>
        public static void Play2D(string eventPath)
        {
            if (string.IsNullOrEmpty(eventPath))
                return;

            try
            {
                SoundEvent.PlaySound2D(eventPath);
            }
            catch (Exception ex)
            {
                // Keep it non-fatal; sound must never crash the game.
                Log.Debug($"[Sound] Failed to play '{eventPath}': {ex}");
            }
        }
    }
}
