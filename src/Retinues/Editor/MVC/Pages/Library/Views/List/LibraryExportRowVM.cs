using System;
using System.IO;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Factions;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Library.Services;
using Retinues.Editor.MVC.Shared.Views;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Library.Views.List
{
    /// <summary>
    /// Row representing an importable export file.
    /// </summary>
    public abstract class LibraryExportRowVM(ListHeaderVM header, ExportLibrary.Entry item)
        : BaseListRowVM(header, item?.FileName ?? string.Empty)
    {
        protected readonly ExportLibrary.Entry Item = item;

        object _xmlTroopImage;
        bool _xmlTroopImageLoaded;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Type Flags                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsLibraryItem => true;

        [DataSourceProperty]
        public override bool IsLibraryFaction => Item?.Kind == ExportKind.Faction;

        [DataSourceProperty]
        public override bool IsLibraryCharacter => Item?.Kind == ExportKind.Character;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Library, Global = true)]
        [DataSourceProperty]
        public override bool IsSelected =>
            EditorState.Instance.LibraryItem != null
            && Item != null
            && string.Equals(
                EditorState.Instance.LibraryItem.FilePath,
                Item.FilePath,
                StringComparison.OrdinalIgnoreCase
            );

        [DataSourceMethod]
        public override void ExecuteSelect()
        {
            EditorState.Instance.LibraryItem = Item;
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

        [DataSourceProperty]
        public abstract object Image { get; }

        /// <summary>
        /// Gets the troop image from the XML export file.
        /// </summary>
        protected object GetXMLTroopImage()
        {
            if (_xmlTroopImageLoaded)
                return _xmlTroopImage;

            _xmlTroopImageLoaded = true;

            var item = Item;
            if (item == null)
                return null;

            if (
                !ExportXMLReader.TryExtractModelCharacterPayloads(item, out var payloads)
                || payloads.Count == 0
            )
                return null;

            var p = payloads[0];

            using var lease = CharacterPreviewLease.LeaseFromPayload(
                p.Payload,
                p.ModelStringId,
                out _
            );
            var c = lease?.Character;
            if (c == null)
                return null;

            // Troop image from the XML-applied stub.
            _xmlTroopImage = c.GetImage(civilian: false);
            return _xmlTroopImage;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Get the sort value for the given sort key.
        /// </summary>
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

        /// <summary>
        /// Checks if this row matches the given filter string.
        /// </summary>
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
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Resolves a faction from its string identifier.
        /// </summary>
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

    /// <summary>
    /// Row representing an importable faction export file.
    /// </summary>
    public sealed class LibraryFactionExportRowVM(ListHeaderVM header, ExportLibrary.Entry item)
        : LibraryExportRowVM(header, item)
    {
        [DataSourceProperty]
        public override object Image => ResolveFaction(Item?.SourceId)?.Image;
    }

    /// <summary>
    /// Row representing an importable character export file.
    /// </summary>
    public sealed class LibraryCharacterExportRowVM(ListHeaderVM header, ExportLibrary.Entry item)
        : LibraryExportRowVM(header, item)
    {
        [DataSourceProperty]
        public override object Image => GetXMLTroopImage();
    }
}
