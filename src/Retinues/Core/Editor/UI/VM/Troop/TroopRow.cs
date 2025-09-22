using Retinues.Core.Game.Wrappers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Troop
{
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
        public ImageIdentifierVM Image => Troop?.Image;

        [DataSourceProperty]
        public string IndentedName
        {
            get
            {
                if (Troop?.IsRetinue == true)
                    return Troop.Name; // Retinue troops are not indented

                var indent = new string(' ', (Troop?.Tier - 1 ?? 0) * 4);
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

        public void Refresh()
        {
            OnPropertyChanged(nameof(DisplayEmptyMessage));
            OnPropertyChanged(nameof(DisplayTroop));
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
