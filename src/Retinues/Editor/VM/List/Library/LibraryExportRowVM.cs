using System;
using System.IO;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Model;
using Retinues.Model.Characters;
using Retinues.Model.Factions;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List.Library
{
    /// <summary>
    /// Row representing an importable export file.
    /// </summary>
    public abstract class LibraryExportRowVM(ListHeaderVM header, MLibrary.Item item)
        : ListRowVM(header, item?.FileName ?? string.Empty)
    {
        protected readonly MLibrary.Item Item = item;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Type Flags                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsLibraryItem => true;

        [DataSourceProperty]
        public override bool IsLibraryFaction => Item?.Kind == MLibraryKind.Faction;

        [DataSourceProperty]
        public override bool IsLibraryCharacter => Item?.Kind == MLibraryKind.Character;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Library)]
        [DataSourceProperty]
        public override bool IsSelected =>
            State.Instance.LibraryItem != null
            && Item != null
            && string.Equals(
                State.Instance.LibraryItem.FilePath,
                Item.FilePath,
                StringComparison.OrdinalIgnoreCase
            );

        [DataSourceMethod]
        public override void ExecuteSelect()
        {
            State.Instance.LibraryItem = Item;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Display                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Name =>
            Item == null
                ? string.Empty
                : (Path.GetFileNameWithoutExtension(Item.FileName) ?? Item.FileName);

        [DataSourceProperty]
        public string Timestamp =>
            Item == null ? string.Empty : Item.CreatedUtc.ToLocalTime().ToString("g");

        /// <summary>
        /// Image data source for ImageIdentifierWidget.
        /// Character: troop portrait. Faction: banner image.
        /// </summary>
        [DataSourceProperty]
        public abstract object Image { get; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal override IComparable GetSortValue(ListSortKey sortKey)
        {
            if (Item == null)
                return string.Empty;

            return sortKey switch
            {
                ListSortKey.Name => Name,
                ListSortKey.Value => Item.CreatedUtc, // "Date" sort
                _ => Name,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Filtering                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal override bool MatchesFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var cmp = StringComparison.OrdinalIgnoreCase;

            if (!string.IsNullOrEmpty(Name) && Name.IndexOf(filter, cmp) >= 0)
                return true;

            if (!string.IsNullOrEmpty(Item?.FileName) && Item.FileName.IndexOf(filter, cmp) >= 0)
                return true;

            if (!string.IsNullOrEmpty(Item?.SourceId) && Item.SourceId.IndexOf(filter, cmp) >= 0)
                return true;

            return false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Shared Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected static IBaseFaction ResolveFaction(string stringId)
        {
            if (string.IsNullOrWhiteSpace(stringId))
                return null;

            var clan = WClan.Get(stringId);
            if (clan != null)
                return clan;

            var kingdom = WKingdom.Get(stringId);
            if (kingdom != null)
                return kingdom;

            var culture = WCulture.Get(stringId);
            if (culture != null)
                return culture;

            return null;
        }
    }

    public sealed class LibraryFactionExportRowVM(ListHeaderVM header, MLibrary.Item item)
        : LibraryExportRowVM(header, item)
    {
        [DataSourceProperty]
        public override object Image
        {
            get
            {
                var f = ResolveFaction(Item?.SourceId);
                return f?.Image;
            }
        }
    }

    public sealed class LibraryCharacterExportRowVM(ListHeaderVM header, MLibrary.Item item)
        : LibraryExportRowVM(header, item)
    {
        [DataSourceProperty]
        public override object Image
        {
            get
            {
                var c = WCharacter.Get(Item?.SourceId);
                if (c == null)
                    return null;

                // Troop image (same as the character list row uses).
                return c.GetImage(civilian: false);
            }
        }
    }
}
