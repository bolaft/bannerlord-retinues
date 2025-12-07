using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Game.Wrappers;
using Retinues.GUI.Helpers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment.Panel
{
    /// <summary>
    /// ViewModel for a single equipment slot, presenting item data and actions.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentSlotVM(EquipmentIndex index) : BaseButtonVM(autoRegister: false)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly EquipmentIndex Index = index;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected override Dictionary<UIEvent, string[]> EventMap =>
            new()
            {
                [UIEvent.Equip] =
                [
                    nameof(ItemText),
                    nameof(ItemTextColor),
                    nameof(IsStaged),
                    nameof(ImageId),
                    nameof(ImageAdditionalArgs),
#if BL13
                    nameof(ImageTextureProviderName),
#else
                    nameof(ImageTypeCode),
#endif
                    nameof(Hint),
                    nameof(EquipChangeHint),
                    nameof(IsEnabled),
                ],
                [UIEvent.Equipment] =
                [
                    nameof(ItemText),
                    nameof(ItemTextColor),
                    nameof(IsStaged),
                    nameof(ImageId),
                    nameof(ImageAdditionalArgs),
#if BL13
                    nameof(ImageTextureProviderName),
#else
                    nameof(ImageTypeCode),
#endif
                    nameof(Hint),
                    nameof(EquipChangeHint),
                    nameof(IsEnabled),
                ],
                [UIEvent.Slot] = [nameof(IsSelected)],
            };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool HasPendingItem => State.EquipData[Index].Equip != null;

        private WItem Item =>
            HasPendingItem
                ? new WItem(State.EquipData[Index].Equip.ItemId)
                : State.Equipment.Get(Index);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /* ━━━━━━━━━ Texts ━━━━━━━━ */

        [DataSourceProperty]
        public string Label =>
            Index switch
            {
                EquipmentIndex.Weapon0 => L.S("weapon_1_slot_text", "Weapon 1"),
                EquipmentIndex.Weapon1 => L.S("weapon_2_slot_text", "Weapon 2"),
                EquipmentIndex.Weapon2 => L.S("weapon_3_slot_text", "Weapon 3"),
                EquipmentIndex.Weapon3 => L.S("weapon_4_slot_text", "Weapon 4"),
                EquipmentIndex.Head => L.S("head_slot_text", "Head"),
                EquipmentIndex.Cape => L.S("cape_slot_text", "Cape"),
                EquipmentIndex.Body => L.S("body_slot_text", "Body"),
                EquipmentIndex.Gloves => L.S("gloves_slot_text", "Gloves"),
                EquipmentIndex.Leg => L.S("leg_slot_text", "Legs"),
                EquipmentIndex.Horse => L.S("horse_slot_text", "Horse"),
                EquipmentIndex.HorseHarness => L.S("horse_harness_slot_text", "Harness"),
                _ => string.Empty,
            };

        /* ━━━━━━━ Item Info ━━━━━━ */

        [DataSourceProperty]
        public string ItemText =>
            Item == null
                ? string.Empty
                : Format.Crop(
                    HasPendingItem
                        ? Item.Name + $" ({State.EquipData[Index].Equip.Remaining}h)"
                        : Item?.Name,
                    ClanScreen.IsStudioMode ? 75 : 50
                );

        [DataSourceProperty]
        public string ItemTextColor => IsStaged ? "#ebaf2fff" : "#F4E1C4FF";

        [DataSourceProperty]
        public bool IsStaged => HasPendingItem;

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

        /* ━━━━━━━━━ Hints ━━━━━━━━ */

        [DataSourceProperty]
        public CharacterEquipmentItemVM Hint =>
            Item?.Base != null ? new CharacterEquipmentItemVM(Item.Base) : null;

        [DataSourceProperty]
        public BasicTooltipViewModel EquipChangeHint =>
            IsStaged
                ? Tooltip.MakeTooltip(
                    null,
                    L.S(
                        "equip_change_tooltip_body",
                        "This is a pending item change.\n\nTo apply the change, select 'Equip troops' from a fief's town menu."
                    )
                )
                : null;

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public override bool IsSelected => State.Slot == Index;

        [DataSourceProperty]
        public override bool IsEnabled
        {
            get
            {
                // Disable mounts for tier 1 troops if disallowed in config
                if (Config.DisallowMountsForT1Troops && State.Troop.Tier <= 1 && !State.Troop.IsHero)
                    if (Index == EquipmentIndex.Horse || Index == EquipmentIndex.HorseHarness)
                        return false;

                // Disable horse harness if no horse equipped
                if (Index == EquipmentIndex.HorseHarness)
                    if (State.Equipment.Get(EquipmentIndex.Horse) == null)
                        return false;

                return true;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Action Bindings                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Select this equipment slot for editing.
        /// </summary>
        [DataSourceMethod]
        public void ExecuteSelect() => State.UpdateSlot(Index);

        /// <summary>
        /// The equipment index represented by this slot button.
        /// </summary>
        public EquipmentIndex SlotIndex => Index;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Refreshers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Refresh selection state when the active slot changes.
        /// </summary>
        public void OnSlotChanged() => OnPropertyChanged(nameof(IsSelected));

        /// <summary>
        /// Refresh all equipment-dependent bindings for this slot.
        /// </summary>
        public void OnEquipmentChanged()
        {
            OnPropertyChanged(nameof(ItemText));
            OnPropertyChanged(nameof(ItemTextColor));
            OnPropertyChanged(nameof(IsStaged));
            OnPropertyChanged(nameof(ImageId));
            OnPropertyChanged(nameof(ImageAdditionalArgs));
#if BL13
            OnPropertyChanged(nameof(ImageTextureProviderName));
#else
            OnPropertyChanged(nameof(ImageTypeCode));
#endif
            OnPropertyChanged(nameof(Hint));
            OnPropertyChanged(nameof(EquipChangeHint));
            OnPropertyChanged(nameof(IsEnabled));
        }

        /// <summary>
        /// Refresh staged equip indicators for this slot.
        /// </summary>
        public void OnEquipChanged() => OnEquipmentChanged();
    }
}
