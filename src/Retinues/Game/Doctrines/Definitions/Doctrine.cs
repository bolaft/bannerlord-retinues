using System.Collections.Generic;
using Retinues.Configuration;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines.Definitions
{
    public sealed class Doctrine(
        string id,
        Category category,
        TextObject name,
        TextObject description,
        string sprite
    )
    {
        /// <summary>
        /// Gets a doctrine by its ID.
        /// </summary>
        public static Doctrine Get(string id) => DoctrinesRegistry.GetDoctrine(id);

        /* ━━━━━━━ Identity ━━━━━━━ */

        public string Id { get; } = id ?? string.Empty;
        public TextObject Name { get; } = name;
        public TextObject Description { get; } = description;

        /* ━━━━━━━━━ Image ━━━━━━━━ */

        public string Sprite { get; } = sprite;

        /* ━━━━━━━ Category ━━━━━━━ */

        public Category Category { get; } = category;
        public int Index => Category.Doctrines.IndexOf(this);

        public Doctrine Previous => Index > 0 ? Category.Doctrines[Index - 1] : null;

        /* ━━━━━━━━━ Costs ━━━━━━━━ */

        public int MoneyCost =>
            !Settings.DoctrinesCostMoney
                ? 0
                : (int)(
                    Index switch
                    {
                        0 => 1000,
                        1 => 5000,
                        2 => 25000,
                        3 => 100000,
                        _ => 100000,
                    } * Settings.DoctrineMoneyCostMultiplier
                );

        public int InfluenceCost =>
            !Settings.DoctrinesCostInfluence
                ? 0
                : (int)(
                    Index switch
                    {
                        0 => 50,
                        1 => 100,
                        2 => 200,
                        3 => 500,
                        _ => 500,
                    } * Settings.DoctrineInfluenceCostMultiplier
                );

        /* ━━━━━━━ Progress ━━━━━━━ */

        public const int ProgressTarget = 100;

        private int _progress = 0;
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

                if (_progress > ProgressTarget)
                    _progress = ProgressTarget;
            }
        }

        /// <summary>
        /// Forcibly sets the progress to a specific value, bypassing state checks.
        /// Used for loading saved data.
        /// </summary>
        public void ForceSet(int value) => _progress = value;

        /* ━━━━━━━━━ State ━━━━━━━━ */

        public bool IsAcquired { get; set; } = false;
        public bool IsUnlocked => Progress >= ProgressTarget;
        public bool IsInProgress => GetState() == State.InProgress;
        public bool IsLocked => GetState() == State.Locked;

        public enum State
        {
            Locked,
            InProgress,
            Unlocked,
            Acquired,
        }

        /// <summary>
        /// Gets the current state of the doctrine.
        /// </summary>
        public State GetState()
        {
            // If this doctrine is acquired, return acquired.
            if (IsAcquired)
                return State.Acquired;

            // If progress is complete, return unlocked.
            if (IsUnlocked)
                return State.Unlocked;

            // If there is a previous doctrine and it is not acquired, this one is locked.
            if (Previous != null && !Previous.IsAcquired)
                return State.Locked;

            // Otherwise, return in progress.
            return State.InProgress;
        }

        /* ━━━━━━━━━ Feats ━━━━━━━━ */

        public List<Feat> Feats { get; } = [];

        /// <summary>
        /// Adds a feat to this doctrine.
        /// </summary>
        public Feat Add(
            string id,
            TextObject name,
            TextObject description,
            int target,
            int worth,
            bool repeatable
        )
        {
            var feat = new Feat(
                id: id,
                name: name,
                description: description,
                doctrine: this,
                target: target,
                worth: worth,
                repeatable: repeatable
            );

            Feats.Add(feat);

            return feat;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Cheats                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [CommandLineFunctionality.CommandLineArgumentFunction("doctrine_unlock", "retinues")]
        public static string DoctrineUnlock(List<string> args)
        {
            if (args.Count < 1)
                return "Usage: doctrine_unlock <doctrine_id>";

            var id = args[0];
            var doctrine = Get(id);

            if (doctrine == null)
                return $"Doctrine '{id}' not found.";

            if (doctrine.IsUnlocked)
                return $"Doctrine '{doctrine.Name}' ({doctrine.Id}) is already unlocked.";

            doctrine.Progress = ProgressTarget;

            return $"Doctrine '{doctrine.Name}' ({doctrine.Id}) unlocked.";
        }
    }
}
