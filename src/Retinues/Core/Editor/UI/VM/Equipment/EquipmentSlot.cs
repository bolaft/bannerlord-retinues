using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Core.Game.Wrappers;
using Retinues.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.Core.Editor.UI.VM.Equipment
{
    /// <summary>
    /// ViewModel for an equipment slot. Handles selection, enable/disable logic, item info, and UI refresh.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentSlotVM(
        EquipmentIndex slot,
        string label,
        WCharacter troop,
        EquipmentEditorVM editor
    ) : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly EquipmentIndex _slot = slot;

        private readonly WCharacter _troop = troop;

        private readonly EquipmentEditorVM _editor = editor;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        private readonly string _label = label;

        [DataSourceProperty]
        public string ButtonLabel => _label;

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        private bool _isSelected;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;

                    // Specific row selection logic
                    if (value)
                        OnSelect();

                    if (_editor.Screen?.EquipmentList is not null)
                        _editor.Screen.EquipmentList.SearchText = ""; // Clear search on selection change

                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get
            {
                // Disable mounts for tier 1 troops if disallowed in config
                if (Config.GetOption<bool>("NoMountForTier1") && _troop.Tier == 1)
                    if (_slot == EquipmentIndex.Horse || _slot == EquipmentIndex.HorseHarness)
                        return false;

                // Disable horse harness if no horse equipped
                if (_slot == EquipmentIndex.HorseHarness)
                    if (_troop.Equipment.GetItem(EquipmentIndex.Horse) == null)
                        return false;

                return true;
            }
        }

        /* ━━━━━━━ Item Info ━━━━━━ */

        [DataSourceProperty]
        public string Name => Item?.Name;

        /* ━━━━━━━━━ Image ━━━━━━━━ */

#if BL13
        [DataSourceProperty]
        public string AdditionalArgs => Item?.Image?.AdditionalArgs;

        [DataSourceProperty]
        public string Id => Item?.Image?.Id;

        [DataSourceProperty]
        public string TextureProviderName => Item?.Image?.TextureProviderName;
#else
        [DataSourceProperty]
        public string ImageId => Item?.Image.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Item?.Image.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Item?.Image.AdditionalArgs;
#endif

        /* ━━━━━━━━━ Hint ━━━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Helpers.Tooltip.MakeItemTooltip(Item);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteSelect() => Select();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem Item => _troop.Equipment.GetItem(_slot);

        public EquipmentIndex Slot => _slot;

        public void Select()
        {
            if (IsSelected)
                return; // No-op if already selected

            _editor.Select(this);
        }

        public void OnSelect()
        {
            _editor.Screen.EquipmentList?.Refresh();
            _editor.Screen.Refresh();
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(Name));
#if BL13
            OnPropertyChanged(nameof(Id));
            OnPropertyChanged(nameof(TextureProviderName));
            OnPropertyChanged(nameof(AdditionalArgs));
#else
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageTypeCode));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
#endif
            OnPropertyChanged(nameof(Hint));
        }
    }
}
