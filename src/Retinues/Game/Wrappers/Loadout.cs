using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Game.Wrappers
{
    public enum EquipmentCategory
    {
        Invalid = 0,
        Battle = 0,
        Civilian = 1,
        Alternate = 2,
    }

    [SafeClass(SwallowByDefault = false)]
    public class WLoadout(WCharacter troop)
    {

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Troop                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WCharacter _troop = troop;

        public WCharacter Troop => _troop;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Roster                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Battle equipment
        public WEquipment Battle
        {
            get => Equipments[(int)EquipmentCategory.Battle];
            set
            {
                var list = Equipments;
                list[(int)EquipmentCategory.Battle] = value;
                Equipments = list;
            }
        }

        // Civilian equipment
        public WEquipment Civilian
        {
            get => Equipments[(int)EquipmentCategory.Civilian];
            set
            {
                var list = Equipments;
                list[(int)EquipmentCategory.Civilian] = value;
                Equipments = list;
            }
        }

        // Alternate equipments (if any)
        public List<WEquipment> Alternates
        {
            get => [.. Equipments.Skip((int)EquipmentCategory.Alternate)];
            set => Equipments = [.. Equipments.Take((int)EquipmentCategory.Alternate), .. value];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Equipment Lists                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets or sets the list of base Equipment objects in this loadout.
        /// </summary>
        public List<Equipment> BaseEquipments
        {
            get =>
                Reflector
                    .GetFieldValue<MBEquipmentRoster>(_troop.Base, "_equipmentRoster")
                    ?.AllEquipments ?? [];
            set
            {    // Shokuho compatibility: collapse to exactly 2 sets (Battle, Civilian)
                if (ModuleChecker.GetModule("Shokuho") != null)
                {
                    // Pick first non-civilian as Battle; if none, create an empty battle set
                    var battle = value.FirstOrDefault(eq => !eq.IsCivilian)
                                ?? WEquipment.FromCode(null, this, 0).Base;

                    value = [battle, battle];
                }

                for (int i = 0; i < value.Count; i++)
                {
#if BL13
                    var want =
                        (i == 1)
                            ? Equipment.EquipmentType.Civilian
                            : Equipment.EquipmentType.Battle;
                    try
                    {
                        Reflector.SetFieldValue(value[i], "_equipmentType", want);
                    }
                    catch { }
#else
                    bool shouldBeCivilian = (i == 1);
                    if (value[i].IsCivilian != shouldBeCivilian)
                    {
                        var src = value[i];
                        var fixedEq = new Equipment(shouldBeCivilian);
                        foreach (var slot in WEquipment.Slots)
                            fixedEq[slot] = src[slot];
                        value[i] = fixedEq;
                    }
#endif
                }

                var roster = new MBEquipmentRoster();
                Reflector.SetFieldValue(roster, "_equipments", new MBList<Equipment>(value));
                Reflector.SetFieldValue(_troop.Base, "_equipmentRoster", roster);
            }
        }

        /// <summary>
        /// Gets or sets the list of equipments in this loadout.
        /// </summary>
        public List<WEquipment> Equipments
        {
            get =>
                [.. BaseEquipments.Select(e => new WEquipment(e, this, BaseEquipments.IndexOf(e)))];
            set => BaseEquipments = [.. value.Select(we => we.Base)];
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Items                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public List<WItem> Items =>
            [.. Equipments.SelectMany(eq => eq.Items).Where(i => i != null)];

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WEquipment Get(int index)
        {
            if (index < 0 || index >= Equipments.Count)
                return null;
            return Equipments[index];
        }

        public EquipmentCategory GetCategory(int index)
        {
            if (index < 0 || index >= Equipments.Count)
                return EquipmentCategory.Invalid;
            return (EquipmentCategory)index;
        }

        public WEquipment CreateAlternate()
        {
            var alternates = Alternates;
            var equipment = WEquipment.FromCode(
                null,
                this,
                alternates.Count + (int)EquipmentCategory.Alternate
            );
            alternates.Add(equipment);
            Alternates = alternates; // re-assign to trigger any bindings
            return equipment;
        }

        public void RemoveAlternate(WEquipment equipment)
        {
            var alternates = Alternates;
            if (alternates.Remove(equipment))
                Alternates = alternates; // re-assign to trigger any bindings
        }

        /// <summary>
        /// Clears all equipments, setting them to default empty equipments.
        /// </summary>
        public void Clear()
        {
            Equipments =
            [
                WEquipment.FromCode(null, this, (int)EquipmentCategory.Battle),
                WEquipment.FromCode(null, this, (int)EquipmentCategory.Civilian),
            ];
        }

        /// <summary>
        /// Fills this loadout from another loadout.
        /// </summary>
        public void FillFrom(WLoadout loadout, bool ordered = false)
        {
            // Origin loadout is already in order
            if (ordered)
            {
                Battle = WEquipment.FromCode(
                    loadout.Battle.Code,
                    this,
                    (int)EquipmentCategory.Battle
                );
                Civilian = WEquipment.FromCode(
                    loadout.Civilian.Code,
                    this,
                    (int)EquipmentCategory.Civilian
                );
                Alternates =
                [
                    .. loadout.Alternates.Select(eq =>
                        WEquipment.FromCode(eq.Code, this, eq.Index)
                    ),
                ];
            }
            // Need to detect categories
            else
            {
                WEquipment battle = null;
                WEquipment civilian = null;

                foreach (var eq in loadout.Equipments)
                {
                    if (eq.IsCivilian && civilian == null)
                        civilian = WEquipment.FromCode(
                            eq.Code,
                            this,
                            (int)EquipmentCategory.Civilian
                        );
                    else if (!eq.IsCivilian && battle == null)
                        battle = WEquipment.FromCode(eq.Code, this, (int)EquipmentCategory.Battle);

                    if (battle != null && civilian != null)
                        break;
                }

                // Replace the entire list so no alternates leak in from source
                Equipments =
                [
                    battle ?? WEquipment.FromCode(null, this, (int)EquipmentCategory.Battle),
                    civilian ?? WEquipment.FromCode(null, this, (int)EquipmentCategory.Civilian),
                ];
            }
        }

        /// <summary>
        /// Gets the skill requirement for a specific skill.
        /// </summary>
        public int ComputeSkillRequirement(SkillObject skill)
        {
            int requirement = 0;

            foreach (var eq in Equipments)
            {
                var req = eq.ComputeSkillRequirement(skill);
                if (req > requirement)
                    requirement = req;
            }

            return requirement;
        }

        /// <summary>
        /// Determines if category 'a' is better than category 'b' for horse items.
        /// </summary>
        private bool IsBetterHorseCategory(ItemCategory a, ItemCategory b)
        {
            if (a == DefaultItemCategories.NobleHorse)
                return b != DefaultItemCategories.NobleHorse;
            else if (a == DefaultItemCategories.WarHorse)
                return b != DefaultItemCategories.WarHorse;
            else if (a != null)
                return b == null;
            else
                return false;
        }

        /// <summary>
        /// Finds the best horse category in this loadout.
        /// </summary>
        private ItemCategory FindBestHorseCategory()
        {
            ItemCategory bestCategory = null;

            foreach (var eq in Equipments)
            {
                var horseItem = eq.Get(EquipmentIndex.Horse);
                if (horseItem != null)
                {
                    var category = horseItem.Category;
                    if (bestCategory == null || IsBetterHorseCategory(category, bestCategory))
                    {
                        bestCategory = category;
                    }
                }
            }

            return bestCategory;
        }

        /// <summary>
        /// Computes the item category requirement for upgrading to the owner troop.
        /// Checks all equipments for horse items, and returns the best one found.
        /// </summary>
        public ItemCategory ComputeUpgradeItemRequirement()
        {
            var bestHorse = FindBestHorseCategory();
            var bestHorseOfParent = Troop.Parent?.Loadout.FindBestHorseCategory();

            if (IsBetterHorseCategory(bestHorse, bestHorseOfParent))
                return bestHorse;
            else
                return null;
        }
    }
}
