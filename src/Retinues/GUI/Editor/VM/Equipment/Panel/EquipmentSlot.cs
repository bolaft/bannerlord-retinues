using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment.Panel
{
    [SafeClass]
    public sealed class EquipmentSlotVM : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public readonly EditorVM Editor;

        public EquipmentSlotVM(EditorVM editor, EquipmentIndex index, string label)
        {
            Log.Info("Building EquipmentSlotVM...");

            Editor = editor;
            _index = index;
            _label = label;
        }

        public void Initialize()
        {
            Log.Info("Initializing EquipmentSlotVM...");

            // Subscribe to events
            void Refresh()
            {
                OnPropertyChanged(nameof(IsStaged));
                OnPropertyChanged(nameof(StagedItem));
                OnPropertyChanged(nameof(ItemText));
                OnPropertyChanged(nameof(ItemTextColor));
                OnPropertyChanged(nameof(Hint));
                OnPropertyChanged(nameof(ImageId));
                OnPropertyChanged(nameof(ImageAdditionalArgs));
#if BL13
                OnPropertyChanged(nameof(ImageTextureProviderName));
#else
                OnPropertyChanged(nameof(ImageTypeCode));
#endif
            }

            EventManager.EquipmentChange.Register(Refresh);
            EventManager.EquipmentItemChange.Register(Refresh);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WEquipment SelectedEquipment => Editor?.EquipmentScreen?.Equipment;
        private WCharacter SelectedTroop => Editor?.TroopScreen?.TroopList?.Selection?.Troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Item                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem Item => SelectedEquipment?.Get(_index);

        private readonly EquipmentIndex _index;
        public EquipmentIndex Index => _index;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Helper
        public bool IsStaged => StagedItem != null;

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        private string _label;

        [DataSourceProperty]
        public string Label => _label;

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        private bool _isSelected;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;

                if (value == true)
                    EventManager.EquipmentSlotChange.Fire();

                OnPropertyChanged(nameof(IsSelected));
            }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get
            {
                // Disable mounts for tier 1 troops if disallowed in config
                if (Config.NoMountForTier1 && (SelectedTroop?.Tier ?? 0) == 1)
                    if (_index == EquipmentIndex.Horse || _index == EquipmentIndex.HorseHarness)
                        return false;

                // Disable horse harness if no horse equipped
                if (_index == EquipmentIndex.HorseHarness)
                    if (SelectedEquipment?.Get(EquipmentIndex.Horse) == null)
                        return false;

                return true;
            }
        }

        /* ━━━━━━━ Item Info ━━━━━━ */

        [DataSourceProperty]
        public string ItemText =>
            Format.Crop(
                IsStaged ? StagedItem?.Name + $" ({StagedChange?.Remaining ?? 0}h)" : Item?.Name,
                25
            );

        [DataSourceProperty]
        public string ItemTextColor => IsStaged ? "#ebaf2fff" : "#F4E1C4FF";

        /* ━━━━━━━━━ Image ━━━━━━━━ */

        [DataSourceProperty]
        public string ImageId => Item?.Image?.Id;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Item?.Image?.AdditionalArgs;

#if BL13
        [DataSourceProperty]
        public string ImageTextureProviderName => Item?.Image?.TextureProviderName;
#else
        [DataSourceProperty]
        public int ImageTypeCode => Item?.Image?.ImageTypeCode ?? 0;
#endif

        /* ━━━━━━━━━ Hint ━━━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Helpers.Tooltip.MakeItemTooltip(Item);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteSelect() => Editor?.EquipmentScreen?.EquipmentPanel?.Select(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Staging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem StagedItem => IsStaged ? new WItem(StagedChange?.ItemId) : null;

        public PendingEquipData StagedChange =>
            TroopEquipBehavior.GetStagedChange(
                SelectedTroop,
                _index,
                SelectedEquipment?.Index ?? 0
            );

        public void Unstage(bool noEvent = false)
        {
            if (!IsStaged)
                return; // No-op if no staged change

            // Restock current item if any
            var item = new WItem(StagedChange?.ItemId);
            item.Stock();

            // Unstage
            TroopEquipBehavior.UnstageChange(SelectedTroop, _index, SelectedEquipment?.Index ?? 0);

            if (noEvent == false)
                EventManager.EquipmentItemChange.Fire();
        }
    }
}
