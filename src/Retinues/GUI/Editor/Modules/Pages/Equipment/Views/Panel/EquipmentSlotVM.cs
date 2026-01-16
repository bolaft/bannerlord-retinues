using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.GUI.Components;
using Retinues.GUI.Editor.Controllers.Equipment;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Services;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Modules.Pages.Equipment.Views.Panel
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

        private WItem Item => PreviewController.GetItem(slot);

        [DataSourceProperty]
        [EventListener(UIEvent.Item, Global = true)]
        public bool IsSlotStaged
        {
            get
            {
                var me = State.Equipment;
                if (me == null)
                    return false;

                // Preferred: MEquipment.IsStaged(EquipmentIndex)
                try
                {
                    var v = Reflection.InvokeMethod(me, "IsStaged", [typeof(EquipmentIndex)], slot);
                    if (v is bool b)
                        return b;
                }
                catch { }

                // Fallback: MEquipment.GetStaged(EquipmentIndex) != null
                try
                {
                    var v = Reflection.InvokeMethod(
                        me,
                        "GetStaged",
                        [typeof(EquipmentIndex)],
                        slot
                    );
                    return v != null;
                }
                catch { }

                return false;
            }
        }

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public int TextWidth =>
            IsSlotStaged && !PreviewController.Enabled
                ? 600 - (24 + 12) // icon size + margin
                : 600;

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public string ItemText => Item?.Name ?? string.Empty;

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public string ItemTextColor =>
            IsSlotStaged && !PreviewController.Enabled ? "#ebaf2fff" : "#F4E1C4FF";

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

        [EventListener(UIEvent.Item)]
        [DataSourceProperty]
        public CharacterEquipmentItemVM Tooltip => Item?.Base != null ? new(Item.Base) : null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Staging                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public Icon StagingIcon =>
            new(
                tooltipFactory: () =>
                    new(
                        L.T(
                                "slot_value_hint_staged",
                                "Actual item until equipping completes: {CURRENT}."
                            )
                            .SetTextVariable(
                                "CURRENT",
                                State.Equipment.GetBase(slot)?.Name ?? L.S(
                                        "no_item_in_slot",
                                        "none"
                                    )
                            )
                    ),
                refresh: [UIEvent.Item],
                visibilityGate: () => IsSlotStaged && !PreviewController.Enabled,
                size: 24
            );

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
                    if (PreviewController.GetItem(EquipmentIndex.Horse) == null)
                        return false; // Harness requires a horse.
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
