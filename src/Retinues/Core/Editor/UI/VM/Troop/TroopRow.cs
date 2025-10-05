using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Troop
{
    /// <summary>
    /// ViewModel for a troop row in the troop list. Handles display, search filtering, and selection refresh logic.
    /// </summary>
    [SafeClass]
    public sealed class TroopRowVM(WCharacter troop, TroopListVM list)
        : BaseRow<TroopListVM, TroopRowVM>(list)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public bool DisplayEmptyMessage => Troop is null;

        [DataSourceProperty]
        public bool DisplayTroop => Troop is not null;

        [DataSourceProperty]
        public string ImageId => Troop?.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Troop?.Image.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Troop?.Image.AdditionalArgs;

        private bool _isVisible = true;

        [DataSourceProperty]
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (_isVisible == value)
                    return;
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }

        [DataSourceProperty]
        public string IndentedName
        {
            get
            {
                if (Troop?.IsRetinue == true || Troop?.IsMilitia == true)
                    return Troop.Name; // Retinue and militia troops are not indented

                int n = Troop?.Tier - 1 ?? 0; // Tier 1 = 0 indents, Tier 2 = 1 indent, etc.
                if (n < 0)
                    n = 0; // Safeguard for tier 0 troops
                var indent = new string(' ', n * 4);
                return $"{indent}{Troop?.Name}"; // Indent based on tier
            }
        }

        [DataSourceProperty]
        public string TierText => $"T{Troop?.Tier}";

        [DataSourceProperty]
        public string EmptyMessage =>
            L.S("acquire_fief_to_unlock", "Acquire a fief to unlock clan troops.");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Troop { get; } = troop;

        public void RefreshVisibility(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                IsVisible = true;
                return;
            }

            if (Troop == null)
            {
                IsVisible = true;
                return;
            }

            var search = searchText.Trim().ToLowerInvariant();
            var name = Troop.Name.ToString().ToLowerInvariant();
            IsVisible = name.Contains(search);
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(DisplayEmptyMessage));
            OnPropertyChanged(nameof(DisplayTroop));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
            OnPropertyChanged(nameof(IndentedName));
            OnPropertyChanged(nameof(TierText));
            OnPropertyChanged(nameof(EmptyMessage));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override void OnSelect()
        {
            RowList.Screen.TroopEditor.Refresh();
            RowList.Screen.Refresh();
        }
    }
}
