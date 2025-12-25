using System;

namespace Retinues.Model
{
    public enum MLibraryKind
    {
        Unknown = 0,
        Character = 1,
        Faction = 2,
    }

    /// <summary>
    /// Metadata for a single export file in the Library.
    /// </summary>
    public sealed class MLibraryItem(
        string filePath,
        string fileName,
        MLibraryKind kind,
        string sourceId,
        DateTime createdUtc,
        int entryCount,
        int troopCount,
        string displayName
    )
    {
        public string FilePath { get; } = filePath ?? string.Empty;
        public string FileName { get; } = fileName ?? string.Empty;
        public MLibraryKind Kind { get; } = kind;
        public string SourceId { get; } = sourceId ?? string.Empty;
        public DateTime CreatedUtc { get; } = createdUtc;

        public int EntryCount { get; } = entryCount;
        public int TroopCount { get; } = troopCount;

        public string DisplayName { get; } = displayName ?? string.Empty;
    }
}
