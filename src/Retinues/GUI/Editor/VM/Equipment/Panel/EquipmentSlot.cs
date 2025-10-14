using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Features.Upgrade.Behaviors;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM.Equipment.List;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment.Panel
{
    [SafeClass]
    public sealed class EquipmentSlotVM(EquipmentIndex index, string label) : BaseComponent
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Item                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem Item => SelectedEquipment.Get(index);

        public EquipmentIndex Index => index;

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
                if (value == true)
                    Editor.EquipmentScreen.EquipmentList = new EquipmentListVM();

                OnPropertyChanged(nameof(IsSelected));
            }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get
            {
                // Disable mounts for tier 1 troops if disallowed in config
                if (Config.NoMountForTier1 && SelectedTroop.Tier == 1)
                    if (index == EquipmentIndex.Horse || index == EquipmentIndex.HorseHarness)
                        return false;

                // Disable horse harness if no horse equipped
                if (index == EquipmentIndex.HorseHarness)
                    if (SelectedEquipment.Get(EquipmentIndex.Horse) == null)
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
        public string ImageAdditionalArgs => Item?.Image?.AdditionalArgs;

        [DataSourceProperty]
        public string ImageId => Item?.Image?.Id;

        [DataSourceProperty]
        public string ImageTextureProviderName => Item?.Image?.TextureProviderName;
#else
        [DataSourceProperty]
        public string ImageId => Item?.Image?.Id;

        [DataSourceProperty]
        public int ImageTypeCode => Item?.Image?.ImageTypeCode ?? 0;

        [DataSourceProperty]
        public string ImageAdditionalArgs => Item?.Image?.AdditionalArgs;
#endif

        /* ━━━━━━━━━ Hint ━━━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Helpers.Tooltip.MakeItemTooltip(Item);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public void ExecuteSelect() => Editor.EquipmentScreen.EquipmentPanel.Select(this);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Staging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem StagedItem => IsStaged ? new WItem(StagedChange.ItemId) : null;

        public PendingEquipData StagedChange =>
            TroopEquipBehavior.GetStagedChange(SelectedTroop, index, SelectedEquipment.Index);

        public void Unstage()
        {
            if (!IsStaged)
                return; // No-op if no staged change

            // Restock current item if any
            var item = new WItem(StagedChange.ItemId);
            item.Stock();

            // Unstage
            TroopEquipBehavior.UnstageChange(SelectedTroop, index, SelectedEquipment.Index);
        }
    }
}
