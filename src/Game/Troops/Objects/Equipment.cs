using System.Linq;
using System.Collections.Generic;
using TaleWorlds.Core;
using CustomClanTroops.Wrappers.Objects;

namespace CustomClanTroops.Game.Troops.Objects
{
    public class TroopEquipment(Equipment equipment) : EquipmentWrapper(equipment)
    {
        // =========================================================================
        // Overrides
        // =========================================================================

        public new List<TroopItem> Items => [.. base.Items.Select(iw => new TroopItem(iw.Base))];

        public new TroopItem WeaponItemBeginSlot
        {
            get => new(base.WeaponItemBeginSlot.Base);
            set => base.WeaponItemBeginSlot = new ItemWrapper(value.Base);
        }
        public new TroopItem Weapon1
        {
            get => new(base.Weapon1.Base);
            set => base.Weapon1 = new ItemWrapper(value.Base);
        }
        public new TroopItem Weapon2
        {
            get => new(base.Weapon2.Base);
            set => base.Weapon2 = new ItemWrapper(value.Base);
        }
        public new TroopItem Weapon3
        {
            get => new(base.Weapon3.Base);
            set => base.Weapon3 = new ItemWrapper(value.Base);
        }
        public new TroopItem Head
        {
            get => new(base.Head.Base);
            set => base.Head = new ItemWrapper(value.Base);
        }
        public new TroopItem Cape
        {
            get => new(base.Cape.Base);
            set => base.Cape = new ItemWrapper(value.Base);
        }
        public new TroopItem Body
        {
            get => new(base.Body.Base);
            set => base.Body = new ItemWrapper(value.Base);
        }
        public new TroopItem Gloves
        {
            get => new(base.Gloves.Base);
            set => base.Gloves = new ItemWrapper(value.Base);
        }
        public new TroopItem Leg
        {
            get => new(base.Leg.Base);
            set => base.Leg = new ItemWrapper(value.Base);
        }
        public new TroopItem Horse
        {
            get => new(base.Horse.Base);
            set => base.Horse = new ItemWrapper(value.Base);
        }
        public new TroopItem HorseHarness
        {
            get => new(base.HorseHarness.Base);
            set => base.HorseHarness = new ItemWrapper(value.Base);
        }

        public new TroopItem GetItem(EquipmentIndex slot)
        {
            return new TroopItem(base.GetItem(slot).Base);
        }
    }
}
