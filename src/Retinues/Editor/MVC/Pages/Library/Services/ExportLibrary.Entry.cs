using System;

namespace Retinues.Editor.MVC.Pages.Library.Services
{
    /// <summary>
    /// Kinds of exports supported in the Library.
    /// </summary>
    public enum ExportKind
    {
        Unknown = 0,
        Character = 1,
        Faction = 2,
    }

    /// <summary>
    /// Library export metadata container.
    /// </summary>
    public static partial class ExportLibrary
    {
        /// <summary>
        /// Metadata for a single export file in the Library.
        /// </summary>
        public sealed class Entry(
            string filePath,
            string fileName,
            ExportKind kind,
            string sourceId,
            DateTime createdUtc,
            int entryCount,
            int troopCount,
            string displayName
        )
        {
            public string FilePath { get; } = filePath ?? string.Empty;
            public string FileName { get; } = fileName ?? string.Empty;
            public ExportKind Kind { get; } = kind;
            public string SourceId { get; } = sourceId ?? string.Empty;
            public DateTime CreatedUtc { get; } = createdUtc;

            public int EntryCount { get; } = entryCount;
            public int TroopCount { get; } = troopCount;

            public string DisplayName { get; } = displayName ?? string.Empty;
        }
    }
}
