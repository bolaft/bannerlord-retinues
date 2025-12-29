using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor.Controllers.Equipment;
using Retinues.Editor.Events;
using Retinues.UI.Services;
using Retinues.UI.VM;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Equipment
{
    /// <summary>
    /// Equipment slot.
    /// </summary>
    public partial class EquipmentSlotVM(EquipmentIndex slot, string label) : EventListenerVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Slot                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string Label => label;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Item                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private WItem Item => State.Equipment.Get(slot);

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
        public object ImageTypeCode => Item?.Image.ImageTypeCode;
# endif

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public object ImageAdditionalArgs => Item?.Image.AdditionalArgs;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Tooltip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        CharacterEquipmentItemVM _tooltip = null;

        [EventListener(UIEvent.Item)]
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
                if (slot == EquipmentIndex.HorseHarness)
                    if (State.Equipment.Get(EquipmentIndex.Horse) == null)
                        return false; // Horse harness requires a horse.
                return true;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // IMPORTANT: selection state must update for all slots when Slot changes,
        // otherwise only the selected slot would refresh and old selections would stick.
        [EventListener(UIEvent.Slot, Global = true)]
        [DataSourceProperty]
        public bool IsSelected => State.Slot == slot;

        [DataSourceMethod]
        public void ExecuteSelect() => State.Slot = slot;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Unequip                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Button<EquipmentIndex> UnequipButton { get; } =
            new(
                action: ItemController.Unequip,
                arg: () => slot,
                refresh: [UIEvent.Item, UIEvent.Slot],
                visibilityGate: () => ItemController.Unequip.Allow(slot)
            );
    }
}
