using System;
using TaleWorlds.Core;

namespace Retinues.Game.Wrappers
{
    public sealed class WEquipmentMirror : WEquipment
    {
        private readonly WEquipment _origin;

        public WEquipmentMirror(Equipment equipment, WEquipment origin) : base(equipment)
        {
            Console.WriteLine($"[LOG] WEquipmentMirror.ctor called: equipment={equipment}, origin={origin}");
            _origin = origin ?? throw new ArgumentNullException(nameof(origin));

            foreach (var slot in Slots)
            {
                if (_equipment[slot].Item is null)
                    SetItem(slot, GetOriginItem(slot)); // mirror origin if empty
            }
        }

        private bool IsMirrored(EquipmentIndex slot)
        {
            Console.WriteLine($"[LOG] WEquipmentMirror.IsMirrored called: slot={slot}");
            // don't use GetItem to avoid recursion
            var item = new WItem(_equipment[slot].Item);
            var originItem = GetOriginItem(slot);
            if (item is null || originItem is null)
                return false;
            return item == originItem;
        }

        public override WItem GetItem(EquipmentIndex slot)
        {
            Console.WriteLine($"[LOG] WEquipmentMirror.GetItem called: slot={slot}");
            var obj = _equipment[slot].Item;
            if (obj == null)
                return null;
            return new WItem(obj, isMirrored: IsMirrored(slot));
        }

        public WItem GetOriginItem(EquipmentIndex slot)
        {
            Console.WriteLine($"[LOG] WEquipmentMirror.GetOriginItem called: slot={slot}");
            return _origin.GetItem(slot);
        }

        public override void SetItem(EquipmentIndex slot, WItem item)
        {
            Console.WriteLine($"[LOG] WEquipmentMirror.SetItem called: slot={slot}, item={item}");
            if (item == null)
                item = GetOriginItem(slot); // revert to origin if null

            _equipment[slot] = new EquipmentElement(item?.Base);
        }

        public override void UnsetItem(EquipmentIndex slot)
        {
            Console.WriteLine($"[LOG] WEquipmentMirror.UnsetItem called: slot={slot}");
            _equipment[slot] = _origin.Base[slot];
        }
    }
}
