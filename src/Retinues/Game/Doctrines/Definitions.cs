using System.Collections.Generic;
using Retinues.Configuration;
using TaleWorlds.Localization;

namespace Retinues.Game.Doctrines
{
    public sealed class Category(string id, TextObject name)
    {
        /// <summary>
        /// Gets a category by its ID.
        /// </summary>
        public static Category Get(string id) => Catalogs.DoctrineCatalog.GetCategory(id);

        /* ━━━━━━━ Identity ━━━━━━━ */

        public string Id { get; } = id ?? string.Empty;
        public TextObject Name { get; } = name;

        /* ━━━━━━━ Doctrines ━━━━━━ */

        public List<Doctrine> Doctrines { get; } = [];

        /// <summary>
        /// Adds a doctrine to this category.
        /// </summary>
        public Doctrine Add(string id, TextObject name, TextObject description)
        {
            var doctrine = new Doctrine(
                id: id,
                category: this,
                name: name,
                description: description
            );

            Doctrines.Add(doctrine);

            return doctrine;
        }
    }

    public sealed class Doctrine(
        string id,
        Category category,
        TextObject name,
        TextObject description
    )
    {
        /// <summary>
        /// Gets a doctrine by its ID.
        /// </summary>
        public static Doctrine Get(string id) => Catalogs.DoctrineCatalog.GetDoctrine(id);

        /* ━━━━━━━ Identity ━━━━━━━ */

        public string Id { get; } = id ?? string.Empty;
        public TextObject Name { get; } = name;
        public TextObject Description { get; } = description;

        /* ━━━━━━━ Category ━━━━━━━ */

        public Category Category { get; } = category;
        public int Index => Category.Doctrines.IndexOf(this);

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

            // Check previous doctrine acquisition for unlocking.
            var previous = Index > 0 ? Category.Doctrines[Index - 1] : null;

            // If there is a previous doctrine and it is not acquired, this one is locked.
            if (previous != null && !previous.IsAcquired)
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
    }

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
        public static Feat Get(string id) => Catalogs.DoctrineCatalog.GetFeat(id);

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
    }
}
