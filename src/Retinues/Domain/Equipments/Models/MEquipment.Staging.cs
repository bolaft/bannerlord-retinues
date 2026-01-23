using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Editor;
using Retinues.Framework.Model.Attributes;
using Retinues.Settings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Equipments.Models
{
    public partial class MEquipment
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Item Staging                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// FIFO queue of staged slot changes, encoded as "slotIndex|itemId".
        /// itemId is empty for unequip.
        /// We keep at most one entry per slot (new stage replaces older for same slot).
        /// </summary>
        MAttribute<List<string>> ItemsStagingAttribute =>
            Attribute(initialValue: new List<string>(), name: "ItemsStaging");

        public List<string> ItemsStaging
        {
            get => [.. ItemsStagingAttribute.Get() ?? []];
            set => ItemsStagingAttribute.Set(value == null ? new() : new(value));
        }

        MAttribute<float> ItemStagingProgressAttribute =>
            Attribute(initialValue: 0f, name: "ItemStagingProgress");

        /// <summary>
        /// Progress accumulator measured in "work hours".
        /// The ItemStagingBehavior adds 1.0 per game hour when active.
        /// </summary>
        public float ItemStagingProgress
        {
            get => ItemStagingProgressAttribute.Get();
            set
            {
                owner.OnEquipmentChange();
                ItemStagingProgressAttribute.Set(value);
            }
        }

        /// <summary>
        /// Returns true if there are any staged items.
        /// </summary>
        public bool HasAnyStagedItems()
        {
            var list = ItemsStagingAttribute.Get();
            return list != null && list.Count > 0;
        }

        /// <summary>
        /// Returns true if item staging is active for the given character.
        /// </summary>
        internal static bool IsItemStagingActive(WCharacter wc)
        {
            if (wc == null || wc.IsHero)
                return false;

            if (!Configuration.EquippingTakesTime)
                return false;

            return EditorState.Instance.Mode == EditorMode.Player;
        }

        /// <summary>
        /// Encodes a staged slot change as "slotIndex|itemId".
        /// </summary>
        private static string EncodeStage(EquipmentIndex slot, string itemId) =>
            $"{(int)slot}|{itemId ?? string.Empty}";

        /// <summary>
        /// Tries to decode a staged slot change from "slotIndex|itemId".
        /// </summary>
        private static bool TryDecodeStage(
            string encoded,
            out EquipmentIndex slot,
            out string itemId
        )
        {
            slot = EquipmentIndex.None;
            itemId = string.Empty;

            if (string.IsNullOrEmpty(encoded))
                return false;

            int sep = encoded.IndexOf('|');
            if (sep <= 0 || sep >= encoded.Length)
                return false;

            var a = encoded.Substring(0, sep);
            var b = encoded.Substring(sep + 1);

            if (!int.TryParse(a, out int slotInt))
                return false;

            if (slotInt < 0 || slotInt >= SlotCount)
                return false;

            slot = (EquipmentIndex)slotInt;
            itemId = b ?? string.Empty;
            return true;
        }

        /// <summary>
        /// Returns true if there is a staged slot change for the specified equipment slot.
        /// </summary>
        private bool HasStagedSlot(EquipmentIndex slot)
        {
            var list = ItemsStagingAttribute.Get();
            if (list == null || list.Count == 0)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (!TryDecodeStage(list[i], out var s, out _))
                    continue;

                if (s == slot)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the staged item ID for the specified equipment slot, if any.
        /// </summary>
        private string GetStagedItemId(EquipmentIndex slot)
        {
            var list = ItemsStagingAttribute.Get();
            if (list == null || list.Count == 0)
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                if (!TryDecodeStage(list[i], out var s, out var id))
                    continue;

                if (s == slot)
                    return id;
            }

            return null;
        }

        /// <summary>
        /// Removes any staged slot change for the specified equipment slot.
        /// </summary>
        private void RemoveStagedSlot(EquipmentIndex slot)
        {
            var current = ItemsStagingAttribute.Get();
            if (current == null || current.Count == 0)
                return;

            bool any = false;
            var next = new List<string>(current.Count);

            for (int i = 0; i < current.Count; i++)
            {
                var e = current[i];
                if (TryDecodeStage(e, out var s, out _) && s == slot)
                {
                    any = true;
                    continue;
                }

                next.Add(e);
            }

            if (!any)
                return;

            ItemsStagingAttribute.Set(next);

            if (next.Count == 0)
                ItemStagingProgressAttribute.Set(0f);

            _formationDirty = true;
            ItemsChanged?.Invoke(this);
            owner.OnEquipmentChange();
        }

        /// <summary>
        /// Stages the item for the specified equipment slot.
        /// </summary>
        internal void Stage(EquipmentIndex slot, WItem item)
        {
            if (item == null)
                return;

            if (!IsValidSlot(slot))
                return;

            var baseId = GetBase(slot)?.StringId ?? string.Empty;
            var newId = item?.StringId ?? string.Empty;

            // Current "final" planned state: staged if exists, else base.
            var stagedId = GetStagedItemId(slot);
            var currentFinalId = stagedId ?? baseId;

            // No change
            if (string.Equals(currentFinalId, newId, StringComparison.Ordinal))
                return;

            var current = ItemsStagingAttribute.Get() ?? [];
            var next = new List<string>(current.Count + 1);

            // Remove any existing entry for this slot.
            for (int i = 0; i < current.Count; i++)
            {
                var e = current[i];
                if (TryDecodeStage(e, out var s, out _) && s == slot)
                    continue;
                next.Add(e);
            }

            // If staging back to base, this is a cancel for that slot.
            if (string.Equals(baseId, newId, StringComparison.Ordinal))
            {
                ItemsStagingAttribute.Set(next);

                if (next.Count == 0)
                    ItemStagingProgressAttribute.Set(0f);

                _formationDirty = true;
                ItemsChanged?.Invoke(this);
                owner.OnEquipmentChange();
                return;
            }

            // Add as last action (FIFO).
            next.Add(EncodeStage(slot, newId));

            ItemsStagingAttribute.Set(next);

            _formationDirty = true;
            ItemsChanged?.Invoke(this);
            owner.OnEquipmentChange();
        }

        /// <summary>
        /// Gets the required work hours to apply the next staged item.
        /// </summary>
        internal float GetNextStagedHours(float timeMultiplier)
        {
            var list = ItemsStagingAttribute.Get();
            if (list == null || list.Count == 0)
                return 0f;

            if (!TryDecodeStage(list[0], out _, out var itemId))
                return 0f;

            if (string.IsNullOrEmpty(itemId))
                return 0f;

            var manager = MBObjectManager.Instance;
            var obj = manager?.GetObject<ItemObject>(itemId);
            if (obj == null)
                return 0f;

            // 1 day per 1000 value, then multiplied by setting multiplier.
            float days = obj.Value / 1000f * MathF.Max(0.01f, timeMultiplier);
            return MathF.Max(0f, days * 24f);
        }

        /// <summary>
        /// Applies the next staged item, if any.
        /// </summary>
        internal bool TryApplyNextStagedItem(
            out EquipmentIndex slot,
            out WItem item,
            out bool unequip
        )
        {
            slot = EquipmentIndex.None;
            item = null;
            unequip = false;

            var current = ItemsStagingAttribute.Get();
            if (current == null || current.Count == 0)
                return false;

            if (!TryDecodeStage(current[0], out slot, out var itemId))
            {
                // Drop malformed entry
                var nextBad = new List<string>(current);
                nextBad.RemoveAt(0);

                owner.OnEquipmentChange();
                ItemsStagingAttribute.Set(nextBad);

                if (nextBad.Count == 0)
                    ItemStagingProgressAttribute.Set(0f);

                return false;
            }

            // Resolve target item
            if (string.IsNullOrEmpty(itemId))
            {
                item = null;
                unequip = true;
            }
            else
            {
                var manager = MBObjectManager.Instance;
                var obj = manager?.GetObject<ItemObject>(itemId);

                if (obj == null)
                {
                    // Mod removed etc: drop this staged entry
                    var nextMissing = new List<string>(current);
                    nextMissing.RemoveAt(0);

                    owner.OnEquipmentChange();
                    ItemsStagingAttribute.Set(nextMissing);

                    if (nextMissing.Count == 0)
                        ItemStagingProgressAttribute.Set(0f);

                    return false;
                }

                item = WItem.Get(obj);
                unequip = false;
            }

            // Apply to real equipment
            SetBase(slot, item);

            // Remove the staged entry (SetReal removed staged slot too, but keep queue FIFO correct)
            var next = ItemsStagingAttribute.Get() ?? [];
            if (next.Count > 0 && TryDecodeStage(next[0], out var s2, out _))
            {
                if (s2 == slot)
                {
                    next = [.. next];
                    next.RemoveAt(0);

                    owner.OnEquipmentChange();
                    ItemsStagingAttribute.Set(next);

                    if (next.Count == 0)
                        ItemStagingProgressAttribute.Set(0f);
                }
            }

            return true;
        }
    }
}
