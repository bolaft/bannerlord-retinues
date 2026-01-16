using System;
using System.Collections.Generic;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Model;
using Retinues.Framework.Model.Attributes;
using Retinues.GUI.Editor;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Equipments.Models
{
    public class MEquipment(Equipment @base, WCharacter owner) : MBase<Equipment>(@base)
    {
        private static readonly int SlotCount = (int)EquipmentIndex.NumEquipmentSetSlots;

        private readonly MEquipmentRoster _roster = owner.EquipmentRoster;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Infos                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public int Value
        {
            get
            {
                int total = 0;

                for (int i = 0; i < SlotCount; i++)
                {
                    var w = Get((EquipmentIndex)i);
                    if (w?.Base != null)
                        total += w.Base.Value;
                }

                return total;
            }
        }

        public float Weight
        {
            get
            {
                float total = 0f;

                for (int i = 0; i < SlotCount; i++)
                {
                    var idx = (EquipmentIndex)i;
                    if (idx == EquipmentIndex.Horse || idx == EquipmentIndex.HorseHarness)
                        continue; // Ignore horse and horse harness weight.

                    var w = Get(idx);
                    if (w?.Base != null)
                        total += w.Base.Weight;
                }

                return total;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Creation                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static MEquipment Create(
            WCharacter owner,
            bool civilian = false,
            MEquipment source = null
        )
        {
            var equipment =
                source == null ? new Equipment() : Equipment.CreateFromEquipmentCode(source.Code);

            equipment ??= new Equipment();

            var me = new MEquipment(equipment, owner)
            {
                EquipmentType = civilian
                    ? Equipment.EquipmentType.Civilian
                    : Equipment.EquipmentType.Battle,
            };

            owner.OnEquipmentChange();
            return me;
        }

        public static MEquipment FromCode(WCharacter owner, string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;

            var equipment = Equipment.CreateFromEquipmentCode(code);
            if (equipment == null)
                return null;

            var me = new MEquipment(equipment, owner);
            owner.OnEquipmentChange();
            return me;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Code                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Code => CodeAttribute.Get();

        MAttribute<string> CodeAttribute =>
            Attribute(
                getter: _ => Base.CalculateEquipmentCode(),
                setter: (_, code) =>
                {
                    if (string.IsNullOrEmpty(code))
                        return;

                    var src = Equipment.CreateFromEquipmentCode(code);
                    if (src == null)
                        return;

                    // Copy only real slots: [0..NumEquipmentSetSlots-1].
                    for (int i = 0; i < SlotCount; i++)
                    {
                        var idx = (EquipmentIndex)i;
                        Base[idx] = src[idx];
                    }

                    owner.OnEquipmentChange();
                }
            );

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Type                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<Equipment.EquipmentType> EquipmentTypeAttribute =>
            Attribute<Equipment.EquipmentType>("_equipmentType", persistent: false);

        public Equipment.EquipmentType EquipmentType
        {
            get => EquipmentTypeAttribute.Get();
            set
            {
                owner.OnEquipmentChange();
                EquipmentTypeAttribute.Set(value);
            }
        }

        MAttribute<bool> IsCivilianAttribute =>
            Attribute(
                getter: _ => EquipmentType == Equipment.EquipmentType.Civilian,
                setter: (_, isCivilian) =>
                    EquipmentType = isCivilian
                        ? Equipment.EquipmentType.Civilian
                        : Equipment.EquipmentType.Battle
            );

        public bool IsCivilian
        {
            get => IsCivilianAttribute.Get();
            set => IsCivilianAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Battle Types                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<bool> FieldBattleSetAttribute => Attribute(initialValue: true);

        public bool FieldBattleSet
        {
            get => IsCivilian || FieldBattleSetAttribute.Get();
            set
            {
                if (IsCivilian)
                    return;
                owner.OnEquipmentChange();
                FieldBattleSetAttribute.Set(value);
            }
        }

        MAttribute<bool> SiegeBattleSetAttribute => Attribute(initialValue: true);

        public bool SiegeBattleSet
        {
            get => IsCivilian || SiegeBattleSetAttribute.Get();
            set
            {
                if (IsCivilian)
                    return;
                owner.OnEquipmentChange();
                SiegeBattleSetAttribute.Set(value);
            }
        }

        MAttribute<bool> NavalBattleSetAttribute => Attribute(initialValue: true);

        public bool NavalBattleSet
        {
            get => IsCivilian || NavalBattleSetAttribute.Get();
            set
            {
                if (IsCivilian)
                    return;
                owner.OnEquipmentChange();
                NavalBattleSetAttribute.Set(value);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Items                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IEnumerable<WItem> Items
        {
            get
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    var item = Get((EquipmentIndex)i);
                    if (item != null)
                        yield return item;
                }
            }
        }

        public WItem GetBase(EquipmentIndex index)
        {
            if (!IsValidSlot(index))
                return null;

            var element = Base[index];
            var item = element.Item;
            return item == null ? null : WItem.Get(item);
        }

        public WItem GetStaged(EquipmentIndex index)
        {
            if (!IsValidSlot(index))
                return null;

            if (!IsItemStagingActive(owner))
                return null;

            var id = GetStagedItemId(index);
            if (string.IsNullOrEmpty(id))
                return null;

            var manager = MBObjectManager.Instance;
            var obj = manager?.GetObject<ItemObject>(id);
            return obj == null ? null : WItem.Get(obj);
        }

        public bool IsStaged(EquipmentIndex index)
        {
            if (!IsValidSlot(index) || !IsItemStagingActive(owner))
                return false;

            // Important: empty itemId means "staged unequip" (legacy/bug) -> treat as NOT staged.
            // This also prevents the UI from tinting a slot as staged when unequipping.
            var id = GetStagedItemId(index);
            return !string.IsNullOrEmpty(id);
        }

        public WItem Get(EquipmentIndex index)
        {
            if (!IsValidSlot(index))
                return null;

            if (!IsItemStagingActive(owner))
                return GetBase(index);

            var staged = GetStaged(index);
            return staged ?? GetBase(index);
        }

        public void Set(EquipmentIndex index, WItem item)
        {
            if (!IsValidSlot(index))
                return;

            // Unequip is always instant and must never be staged.
            if (item == null)
            {
                // If the slot currently has a staged equip, treat "unequip" as "unstage".
                if (IsItemStagingActive(owner) && HasStagedSlot(index))
                {
                    RemoveStagedSlot(index);
                    return;
                }

                SetReal(index, null);
                return;
            }

            // Equip path
            if (!IsItemStagingActive(owner))
            {
                SetReal(index, item);
                return;
            }

            Stage(index, item);
        }

        private void SetReal(EquipmentIndex index, WItem item)
        {
            if (!IsValidSlot(index))
                return;

            var element = item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);
            Base[index] = element;

            // If we just applied a real item, any staged plan for this slot is now obsolete.
            RemoveStagedSlot(index);

            _formationDirty = true;
            ItemsChanged?.Invoke(this);

            owner.OnEquipmentChange();
        }

        private static bool IsValidSlot(EquipmentIndex index)
        {
            int i = (int)index;
            return i >= 0 && i < SlotCount;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Item Staging                     //
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

        public bool HasAnyStagedItems()
        {
            var list = ItemsStagingAttribute.Get();
            return list != null && list.Count > 0;
        }

        internal static bool IsItemStagingActive(WCharacter wc)
        {
            if (wc == null || wc.IsHero)
                return false;

            if (!Settings.EquippingTakesTime)
                return false;

            return EditorState.Instance.Mode == EditorMode.Player;
        }

        private static string EncodeStage(EquipmentIndex slot, string itemId) =>
            $"{(int)slot}|{itemId ?? string.Empty}";

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
            SetReal(slot, item);

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Roster                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Slot-aware: returns true if setting this slot to item does not require increasing
        /// the roster stock for this item (because another equipment already needs that many).
        /// </summary>
        public bool IsAvailableInRoster(EquipmentIndex slot, WItem item)
        {
            if (item == null)
                return true;

            if (_roster == null)
                return false;

            string id = item.StringId;
            if (string.IsNullOrEmpty(id))
                return false;

            var old = Get(slot);
            if (old != null && old.StringId == id)
            {
                // No net change for this item.
                return true;
            }

            int thisCount = CountInThisEquipment(id);
            int newCount = thisCount + 1;

            int otherMax = _roster.GetMaxCountExcludingEquipment(Base, id);

            // If someone else already requires >= newCount, roster already has enough copies.
            return otherMax >= newCount;
        }

        /// <summary>
        /// Non-slot-aware convenience: assumes you are adding one more copy of item somewhere.
        /// Prefer the slot-aware overload in equip actions.
        /// </summary>
        public bool IsAvailableInRoster(WItem item)
        {
            if (item == null)
                return true;

            if (_roster == null)
                return false;

            string id = item.StringId;
            if (string.IsNullOrEmpty(id))
                return false;

            int thisCount = CountInThisEquipment(id);
            int otherMax = _roster.GetMaxCountExcludingEquipment(Base, id);

            return otherMax >= thisCount + 1;
        }

        private int CountInThisEquipment(string itemId)
        {
            int count = 0;

            for (int i = 0; i < SlotCount; i++)
            {
                var w = Get((EquipmentIndex)i);
                if (w != null && w.StringId == itemId)
                    count++;
            }

            return count;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public event Action<MEquipment> ItemsChanged;

        private bool _formationDirty = true;
        private FormationClassHelper.FormationInfo _formationInfo;

        public FormationClassHelper.FormationInfo FormationInfo
        {
            get
            {
                if (_formationDirty)
                {
                    _formationInfo = FormationClassHelper.Compute(this);
                    _formationDirty = false;
                }

                return _formationInfo;
            }
        }

        public FormationClass FormationClass => FormationInfo.FormationClass;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void MarkAllAttributesDirty()
        {
            EnsureAttributesCreated();

            foreach (var obj in _attributes.Values)
            {
                if (obj is IMAttribute attr)
                    attr.MarkDirty();
            }
        }
    }
}
