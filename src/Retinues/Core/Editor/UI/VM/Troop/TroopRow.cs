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
        public string ImageId => Troop.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Troop.Image.ImageTypeCode;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Troop.Image.AdditionalArgs;

        [DataSourceProperty]
        public string IndentedName
        {
            get
            {
                if (Troop.IsRetinue)
                    return Troop.Name; // Retinue troops are not indented

                var indent = new string(' ', (Troop.Tier - 1) * 4);
                return $"{indent}{Troop.Name}";  // Indent based on tier
            }
        }

        [DataSourceProperty]
        public string TierText => $"T{Troop.Tier}";

        // =========================================================================
        // Public API
        // =========================================================================

        public WCharacter Troop { get; } = troop;

        public void Refresh()
        {
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
            OnPropertyChanged(nameof(IndentedName));
            OnPropertyChanged(nameof(TierText));
        }

        // =========================================================================
        // Internals
        // =========================================================================

        protected override void OnSelect()
        {
            Log.Debug($"Selected troop: {Troop.Name}.");

            RowList.Screen.TroopEditor.Refresh();
            RowList.Screen.Refresh();
        }
    }
}