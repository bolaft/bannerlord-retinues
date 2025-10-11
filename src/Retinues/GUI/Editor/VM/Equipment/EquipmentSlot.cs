using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment
{
    /// <summary>
    /// ViewModel for an equipment slot. Handles selection, enable/disable logic, item info, and UI refresh.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentSlotVM(
        EquipmentIndex slot,
        string label,
        WCharacter troop,
        EquipmentPanelVM editor
    ) : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly EquipmentIndex _slot = slot;

        private readonly WCharacter _troop = troop;

        private readonly EquipmentPanelVM _editor = editor;

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
                    if (_editor.Equipment?.GetItem(EquipmentIndex.Horse) == null)
                        return false;

                return true;
            }
        }

        /* ━━━━━━━ Item Info ━━━━━━ */

        [DataSourceProperty]
        public string Name => Format.Crop(Item?.Name, 25);

        [DataSourceProperty]
        public string StagedName =>
            $"{Format.Crop(StagedItem?.Name, 25)} ({StagedChangeDuration}h)";

        [DataSourceProperty]
        public bool IsStaged => StagedItem != null;

        [DataSourceProperty]
        public bool IsActual => IsStaged == false;

        [DataSourceProperty]
        public bool IsMirrored => false; // Not implemented yet

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

        public PendingEquipData StagedChange =>
            TroopEquipBehavior.GetStagedChange(
                _troop,
                _slot,
                _editor.LoadoutCategory,
                _editor.LoadoutIndex
            );

        public int StagedChangeDuration => StagedChange?.Remaining ?? 0;

        public WItem Item => IsStaged ? StagedItem : ActualItem;

        public WItem ActualItem => _editor.Equipment.GetItem(_slot);

        public WItem StagedItem
        {
            get
            {
                var itemId = StagedChange?.ItemId;
                return itemId is null ? null : new WItem(itemId);
            }
        }

        public EquipmentIndex Slot => _slot;

        public void Unstage()
        {
            if (!IsStaged)
                return; // No-op if no staged change

            var change = TroopEquipBehavior.GetStagedChange(
                _troop,
                _slot,
                _editor.LoadoutCategory,
                _editor.LoadoutIndex
            );
            var item = new WItem(change.ItemId);
            item.Stock();
            TroopEquipBehavior.UnstageChange(
                _troop,
                _slot,
                _editor.LoadoutCategory,
                _editor.LoadoutIndex
            );
        }

        public void Select()
        {
            if (IsSelected)
                return; // No-op if already selected

            _editor.Select(this);
        }

        public void OnSelect() { }
    }
}
