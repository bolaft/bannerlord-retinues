using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
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
        WCharacter troop,
        WEquipment equipment,
        EquipmentIndex slot,
        string label
    ) : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WCharacter _troop = troop;
        private readonly WEquipment _equipment = equipment;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Item                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public EquipmentIndex EquipmentIndex = slot;

        public WItem Item => _equipment.GetItem(EquipmentIndex);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string Label = label;

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

                // Update equipment list on selection
                if (value)
                {
                    Editor.EquipmentList = new EquipmentListVM(
                        _troop,
                        Editor.Faction,
                        EquipmentIndex,
                        civilian: Editor.EquipmentPanel.EquipmentCategory
                            == EquipmentCategory.Civilian
                    );
                    OnPropertyChanged(nameof(Editor.EquipmentList));
                }

                OnPropertyChanged(nameof(IsSelected));
            }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get
            {
                // Disable mounts for tier 1 troops if disallowed in config
                if (Config.NoMountForTier1 && _troop.Tier == 1)
                    if (
                        EquipmentIndex == EquipmentIndex.Horse
                        || EquipmentIndex == EquipmentIndex.HorseHarness
                    )
                        return false;

                // Disable horse harness if no horse equipped
                if (EquipmentIndex == EquipmentIndex.HorseHarness)
                    if (_equipment.GetItem(EquipmentIndex.Horse) == null)
                        return false;

                return true;
            }
        }

        /* ━━━━━━━ Item Info ━━━━━━ */

        [DataSourceProperty]
        public string ItemText =>
            Format.Crop(
                IsStaged ? StagedItem?.Name + $" ({StagedChange.Remaining}h)" : Item?.Name,
                25
            );

        [DataSourceProperty]
        public bool IsStaged => StagedItem != null;

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
        public void ExecuteSelect() => Editor.EquipmentPanel.Select(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Staging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem StagedItem => IsStaged ? new WItem(StagedChange.ItemId) : null;

        public PendingEquipData StagedChange =>
            TroopEquipBehavior.GetStagedChange(
                _troop,
                EquipmentIndex,
                Editor.EquipmentPanel.EquipmentCategory,
                Editor.EquipmentPanel.Index
            );

        public void Unstage()
        {
            if (!IsStaged)
                return; // No-op if no staged change

            // Restock current item if any
            var item = new WItem(StagedChange.ItemId);
            item.Stock();

            // Unstage
            TroopEquipBehavior.UnstageChange(
                _troop,
                EquipmentIndex,
                Editor.EquipmentPanel.EquipmentCategory,
                Editor.EquipmentPanel.Index
            );
        }
    }
}
