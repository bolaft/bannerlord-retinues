using System;
using System.Collections.Generic;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Helpers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Model;
using Retinues.Framework.Model.Attributes;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Domain.Equipments.Models
{
    public partial class MEquipment(Equipment @base, WCharacter owner) : MBase<Equipment>(@base)
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

        /// <summary>
        /// Creates a new equipment instance for the given owner.
        /// </summary>
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

        /// <summary>
        /// Creates a new equipment instance for the given owner from an equipment code.
        /// </summary>
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
                },
                name: "CodeAttribute"
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
                        : Equipment.EquipmentType.Battle,
                name: "IsCivilianAttribute"
            );

        public bool IsCivilian
        {
            get => IsCivilianAttribute.Get();
            set => IsCivilianAttribute.Set(value);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Battle Types                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<bool> FieldBattleSetAttribute =>
            Attribute(initialValue: true, name: "FieldBattleSetAttribute");

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

        MAttribute<bool> SiegeBattleSetAttribute =>
            Attribute(initialValue: true, name: "SiegeBattleSetAttribute");

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

        MAttribute<bool> NavalBattleSetAttribute =>
            Attribute(initialValue: true, name: "NavalBattleSetAttribute");

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

        /// <summary>
        /// Gets the effective item for the specified equipment slot, considering staging.
        /// </summary>
        public WItem Get(EquipmentIndex index)
        {
            if (!IsValidSlot(index))
                return null;

            if (!IsItemStagingActive(owner))
                return GetBase(index);

            var staged = GetStaged(index);
            return staged ?? GetBase(index);
        }

        /// <summary>
        /// Gets the base (real) item for the specified equipment slot.
        /// </summary>
        public WItem GetBase(EquipmentIndex index)
        {
            if (!IsValidSlot(index))
                return null;

            var element = Base[index];
            var item = element.Item;
            return item == null ? null : WItem.Get(item);
        }

        /// <summary>
        /// Gets the staged item for the specified equipment slot, if any.
        /// </summary>
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

        /// <summary>
        /// Returns true if there is a staged item for the specified equipment slot.
        /// </summary>
        public bool IsStaged(EquipmentIndex index)
        {
            if (!IsValidSlot(index) || !IsItemStagingActive(owner))
                return false;

            // Important: empty itemId means "staged unequip" (legacy/bug) -> treat as NOT staged.
            // This also prevents the UI from tinting a slot as staged when unequipping.
            var id = GetStagedItemId(index);
            return !string.IsNullOrEmpty(id);
        }

        /// <summary>
        /// Sets the item for the specified equipment slot, considering staging.
        /// </summary>
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

                SetBase(index, null);
                return;
            }

            // Equip path
            if (!IsItemStagingActive(owner))
            {
                SetBase(index, item);
                return;
            }

            Stage(index, item);
        }

        /// <summary>
        /// Sets the base (real) item for the specified equipment slot.
        /// </summary>
        private void SetBase(EquipmentIndex index, WItem item)
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

        /// <summary>
        /// Validates that the given equipment slot index is within bounds.
        /// </summary>
        private static bool IsValidSlot(EquipmentIndex index)
        {
            int i = (int)index;
            return i >= 0 && i < SlotCount;
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

        /// <summary>
        /// Marks all equipment attributes as dirty.
        /// </summary>
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
