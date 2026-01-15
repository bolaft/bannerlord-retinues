using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines.Definitions
{
    /// <summary>
    /// A feat that can be achieved to progress in a doctrine.
    /// </summary>
    public sealed class Feat(
        string id,
        TextObject name,
        TextObject description,
        Doctrine doctrine,
        int target,
        int worth,
        bool repeatable
    )
    {
        /// <summary>
        /// Gets a feat by its ID.
        /// </summary>
        public static Feat Get(string id) => DoctrinesRegistry.GetFeat(id);

        /* ━━━━━━━ Identity ━━━━━━━ */

        public string Id { get; } = id ?? string.Empty;
        public TextObject Name { get; } = name;
        public TextObject Description { get; } = description;

        /* ━━━━━━━ Doctrine ━━━━━━━ */

        public Doctrine Doctrine { get; } = doctrine!;

        /* ━━━━━━━ Progress ━━━━━━━ */

        public int Worth { get; } = worth < 1 ? 1 : worth;
        public bool Repeatable { get; } = repeatable;
        public int Target { get; } = target < 1 ? 1 : target;

        int _progress = 0;
        public int Progress
        {
            get => _progress;
            set
            {
                if (!IsInProgress)
                    return;

                if (value < 0)
                    value = 0;

                _progress = value;

                if (_progress > Target)
                {
                    int completions = 0;
                    if (Repeatable)
                    {
                        while (_progress > Target)
                        {
                            _progress -= Target;
                            completions++;
                        }
                    }
                    else
                    {
                        completions = 1;
                        _progress = Target;
                    }

                    // First input: signal behavior.
                    DoctrinesBehavior.Instance?.OnFeatCompleted(this, completions);

                    // Then: apply worth to doctrine progress.
                    Doctrine.Progress += Worth * completions;
                }
            }
        }

        public void Set(int value, bool bestOnly = false) =>
            Progress = bestOnly && value < Progress ? Progress : value;

        public void Add(int amount = 1) => Progress += amount;

        public void Reset() => Progress = 0;

        public void Complete() => Progress = Target;

        /// <summary>
        /// Forcibly sets the progress to a specific value, bypassing state checks.
        /// Used for loading saved data.
        /// </summary>
        public void ForceSet(int value) => _progress = value;

        /* ━━━━━━━━━ State ━━━━━━━━ */

        public bool IsLocked => GetState() == State.Locked;
        public bool IsInProgress => GetState() == State.InProgress;
        public bool IsCompleted => GetState() == State.Completed;

        public enum State
        {
            Locked,
            InProgress,
            Completed,
        }

        /// <summary>
        /// Gets the current state of the feat.
        /// </summary>
        public State GetState()
        {
            // If progress meets or exceeds target, return completed.
            if (Progress >= Target)
                return State.Completed;

            // If the doctrine is in progress, return in progress.
            if (Doctrine.IsInProgress)
                return State.InProgress;

            // Otherwise, return locked.
            return State.Locked;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cheats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("feat_complete", "retinues")]
        public static string FeatComplete(List<string> args)
        {
            if (args.Count < 1)
                return "Usage: feat_complete <feat_id> [times]";

            var id = args[0];
            var feat = Feat.Get(id);

            if (feat == null)
                return $"Feat '{id}' not found.";

            if (feat.IsCompleted)
                return $"Feat '{feat.Name}' ({feat.Id}) is already completed.";

            feat.Progress = feat.Target;

            return $"Feat '{feat.Name}' ({feat.Id}) completed.";
        }
    }
}
