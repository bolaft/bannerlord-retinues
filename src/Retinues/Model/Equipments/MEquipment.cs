using System.Collections.Generic;
using TaleWorlds.Core;

namespace Retinues.Model.Equipments
{
    public class MEquipment(Equipment @base) : MBase<Equipment>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Code => Base.CalculateEquipmentCode();

        public Equipment.EquipmentType EquipmentType
        {
            get => Attribute<Equipment.EquipmentType>("_equipmentType").Get();
            set => Attribute<Equipment.EquipmentType>("_equipmentType").Set(value);
        }

        public bool IsCivilian
        {
            get => EquipmentType == Equipment.EquipmentType.Civilian;
            set =>
                EquipmentType = value
                    ? Equipment.EquipmentType.Civilian
                    : Equipment.EquipmentType.Battle;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Items API                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the item equipped in the given slot.
        /// </summary>
        public WItem GetItem(EquipmentIndex index)
        {
            var element = Base[index];
            var item = element.Item;
            return item == null ? null : WItem.Get(item);
        }

        /// <summary>
        /// Sets the item equipped in the given slot.
        /// </summary>
        public void SetItem(EquipmentIndex index, WItem item)
        {
            var element = item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);

            Base[index] = element;
        }

        /// <summary>
        /// Enumerates all items in this equipment.
        /// </summary>
        public IEnumerable<WItem> Items()
        {
            for (int i = 0; i < Equipment.EquipmentSlotLength; i++)
            {
                var idx = (EquipmentIndex)i;
                yield return GetItem(idx);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Convenience Slots                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem Weapon0
        {
            get => GetItem(EquipmentIndex.Weapon0);
            set => SetItem(EquipmentIndex.Weapon0, value);
        }

        public WItem Weapon1
        {
            get => GetItem(EquipmentIndex.Weapon1);
            set => SetItem(EquipmentIndex.Weapon1, value);
        }

        public WItem Weapon2
        {
            get => GetItem(EquipmentIndex.Weapon2);
            set => SetItem(EquipmentIndex.Weapon2, value);
        }

        public WItem Weapon3
        {
            get => GetItem(EquipmentIndex.Weapon3);
            set => SetItem(EquipmentIndex.Weapon3, value);
        }

        public WItem ExtraWeapon
        {
            get => GetItem(EquipmentIndex.ExtraWeaponSlot);
            set => SetItem(EquipmentIndex.ExtraWeaponSlot, value);
        }

        public WItem Head
        {
            get => GetItem(EquipmentIndex.Head);
            set => SetItem(EquipmentIndex.Head, value);
        }

        public WItem Body
        {
            get => GetItem(EquipmentIndex.Body);
            set => SetItem(EquipmentIndex.Body, value);
        }

        public WItem Leg
        {
            get => GetItem(EquipmentIndex.Leg);
            set => SetItem(EquipmentIndex.Leg, value);
        }

        public WItem Gloves
        {
            get => GetItem(EquipmentIndex.Gloves);
            set => SetItem(EquipmentIndex.Gloves, value);
        }

        public WItem Cape
        {
            get => GetItem(EquipmentIndex.Cape);
            set => SetItem(EquipmentIndex.Cape, value);
        }

        public WItem Horse
        {
            get => GetItem(EquipmentIndex.Horse);
            set => SetItem(EquipmentIndex.Horse, value);
        }

        public WItem HorseHarness
        {
            get => GetItem(EquipmentIndex.HorseHarness);
            set => SetItem(EquipmentIndex.HorseHarness, value);
        }
    }
}
