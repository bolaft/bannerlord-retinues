using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Game.Wrappers;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Equipment.Panel
{
    /// <summary>
    /// ViewModel for a single equipment slot, presenting item data and actions.
    /// </summary>
    [SafeClass]
    public sealed class EquipmentSlotVM(EquipmentIndex index) : BaseButtonVM
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
                    nameof(IsEnabled),
                ],
                [UIEvent.Equipment] =
                [
                    nameof(ItemText), nameof(ItemTextColor), nameof(IsStaged),
                    nameof(ImageId), nameof(ImageAdditionalArgs),
#if BL13
                    nameof(ImageTextureProviderName),
#else
                    nameof(ImageTypeCode),
#endif
                    nameof(Hint), nameof(IsEnabled),
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
                    25
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

        /* ━━━━━━━━━ Hint ━━━━━━━━━ */

        [DataSourceProperty]
        public BasicTooltipViewModel Hint => Helpers.Tooltip.MakeItemTooltip(Item);

        /* ━━━━━━━━━ Flags ━━━━━━━━ */

        [DataSourceProperty]
        public override bool IsSelected => State.Slot == Index;

        [DataSourceProperty]
        public override bool IsEnabled
        {
            get
            {
                // Disable mounts for tier 1 troops if disallowed in config
                if (Config.NoMountForTier1 && State.Troop.Tier == 1)
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

        [DataSourceMethod]
        /// <summary>
        /// Select this equipment slot for editing.
        /// </summary>
        public void ExecuteSelect() => State.UpdateSlot(Index);
    }
}
