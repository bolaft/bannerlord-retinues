using System;
using Retinues.Utilities;
using TaleWorlds.Engine;

namespace Retinues.Engine
{
    /// <summary>
    /// Centralised helpers for playing sound events.
    /// Usage: Sounds.QuestFinished.Play();
    /// </summary>
    [SafeClass]
    public static class Sounds
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Handles                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// UI notification: quest finished.
        /// </summary>
        public static SoundHandle QuestFinished { get; } =
            new SoundHandle("event:/ui/notification/quest_finished");

        /// <summary>
        /// UI: kingdom / reign decision sound.
        /// </summary>
        public static SoundHandle ReignDecision { get; } =
            new SoundHandle("event:/ui/reign/decision");

        /// <summary>
        /// UI notification: trait change.
        /// </summary>
        public static SoundHandle TraitChange { get; } =
            new SoundHandle("event:/ui/notification/trait_change");

        /// <summary>
        /// UI notification: education.
        /// </summary>
        public static SoundHandle Education { get; } =
            new SoundHandle("event:/ui/notification/education");

        // Add more as needed:
        // public static SoundHandle SomeOtherEvent { get; } =
        //     new SoundHandle("event:/ui/...");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Utilities                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates an ad-hoc sound handle from a raw FMOD event path.
        /// </summary>
        public static SoundHandle FromPath(string eventPath)
        {
            return new SoundHandle(eventPath);
        }
    }

    /// <summary>
    /// Lightweight wrapper around a sound event path.
    /// </summary>
    public readonly struct SoundHandle
    {
        public string EventPath { get; }

        internal SoundHandle(string eventPath)
        {
            EventPath = eventPath ?? string.Empty;
        }

        /// <summary>
        /// Plays this sound as a 2D UI sound.
        /// </summary>
        public void Play()
        {
            if (string.IsNullOrEmpty(EventPath))
                return;

            try
            {
                SoundEvent.PlaySound2D(EventPath);
            }
            catch (Exception ex)
            {
                // Keep it non-fatal; sound must never crash the game.
                Log.Exception(ex, $"SoundHandle.Play failed for event: {EventPath}");
            }
        }
    }
}
