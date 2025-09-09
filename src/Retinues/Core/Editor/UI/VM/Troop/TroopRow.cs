using TaleWorlds.Library;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;

namespace Retinues.Core.Editor.UI.VM.Troop
{
    public sealed class TroopRowVM(WCharacter troop, TroopListVM list) : BaseRow<TroopListVM, TroopRowVM>(list), IView
    {
        // =========================================================================
        // Data Bindings
        // =========================================================================

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

        [DataSourceProperty]
        public string IndentedName
        {
            get
            {
                if (Troop?.IsRetinue == true)
                    return Troop.Name; // Retinue troops are not indented

                var indent = new string(' ', (Troop?.Tier - 1 ?? 0) * 4);
                return $"{indent}{Troop?.Name}";  // Indent based on tier
            }
        }

        [DataSourceProperty]
        public string TierText => $"T{Troop?.Tier}";

        [DataSourceProperty]
        public string EmptyMessage => "Acquire a fief to unlock clan troops.";

        // =========================================================================
        // Public API
        // =========================================================================

        public WCharacter Troop { get; } = troop;

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

        // =========================================================================
        // Internals
        // =========================================================================

        protected override void OnSelect()
        {
            Log.Debug($"Selected troop: {Troop?.Name}.");

            RowList.Screen.TroopEditor.Refresh();
            RowList.Screen.Refresh();
        }
    }
}