using System.Diagnostics.Tracing;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Controllers;
using Retinues.Engine;
using Retinues.Model.Equipments;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Equipment
{
    /// <summary>
    /// Equipment slot.
    /// </summary>
    public partial class EquipmentSlotVM(EquipmentIndex slot, string label) : BaseVM
    {
        private readonly EquipmentIndex _slot = slot;
        private readonly string _label = label;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Slot                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Label => _label;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Item                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WItem Item => State.Equipment.GetItem(_slot);

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public string ItemText => Item?.Name ?? string.Empty;

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public string ItemTextColor => "#F4E1C4FF";

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public object ItemImage => Item?.Image;

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public object ImageId => Item?.Image.Id;

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
# if BL13
        public object ImageTextureProviderName => Item?.Image.TextureProviderName;
# else
        public object ImageTypeCode => Item?.Image.TypeCode;
# endif

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public object ImageAdditionalArgs => Item?.Image.AdditionalArgs;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Tooltip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        CharacterEquipmentItemVM _tooltip = null;

        [DataSourceProperty]
        public CharacterEquipmentItemVM Tooltip =>
            Item == null ? null : _tooltip ??= new CharacterEquipmentItemVM(Item.Base);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Enabled                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public bool IsEnabled
        {
            get
            {
                if (_slot == EquipmentIndex.HorseHarness)
                    if (State.Equipment.GetItem(EquipmentIndex.Horse) == null)
                        return false; // Horse harness requires a horse.
                return true;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Slot)]
        [DataSourceProperty]
        public bool IsSelected => State.Slot == _slot;

        [DataSourceMethod]
        public void ExecuteSelect() => State.Slot = _slot;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unequip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public bool CanUnequip => Item != null;

        [DataSourceProperty]
        public Tooltip UnequipTooltip => new(L.T("unequip_item_tooltip", "Unequip this item."));

        [DataSourceMethod]
        public void ExecuteUnequip() => EquipmentController.UnequipItem(_slot);
    }
}
