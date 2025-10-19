using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Retinues.Utils;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Game.Wrappers
{
    [SafeClass(SwallowByDefault = false)]
    public class WLoadout(WCharacter wc)
    {
        // Owner character
        private readonly WCharacter _owner = wc;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                           Enum                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Equipment categories
        public enum Category
        {
            Battle = 0,
            Civilian = 1,
            Alternate = 2,
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Roster                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Battle equipment
        public WEquipment Battle
        {
            get => Equipments[(int)Category.Battle];
            set
            {
                var list = Equipments;
                list[(int)Category.Battle] = value;
                Equipments = list;
            }
        }

        // Civilian equipment
        public WEquipment Civilian
        {
            get => Equipments[(int)Category.Civilian];
            set
            {
                var list = Equipments;
                list[(int)Category.Civilian] = value;
                Equipments = list;
            }
        }

        // Alternate equipments (if any)
        public List<WEquipment> Alternates
        {
            get => Equipments.Skip((int)Category.Alternate).ToList();
            set => Equipments = [.. Equipments.Take((int)Category.Alternate), .. value];
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
                    .GetFieldValue<MBEquipmentRoster>(_owner.Base, "_equipmentRoster")
                    ?.AllEquipments ?? [];
            set
            {
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
                Reflector.SetFieldValue(_owner.Base, "_equipmentRoster", roster);
            }
        }

        /// <summary>
        /// Gets or sets the list of equipments in this loadout.
        /// </summary>
        public List<WEquipment> Equipments
        {
            get => [.. BaseEquipments.Select(e => new WEquipment(e))];
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

        /// <summary>
        /// Gets the equipment for the specified category and index.
        /// </summary>
        public WEquipment Get(Category category, int index = 0)
        {
            return category switch
            {
                Category.Battle => Battle,
                Category.Civilian => Civilian,
                Category.Alternate => (index >= 0 && index < Alternates.Count)
                    ? Alternates[index]
                    : null,
                _ => null,
            };
        }

        /// <summary>
        /// Clears all equipments, setting them to default empty equipments.
        /// </summary>
        public void Clear()
        {
            Equipments =
            [
                WEquipment.FromCode(null), // Battle
                WEquipment.FromCode(null, civilian: true), // Civilian
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
                Battle = WEquipment.FromCode(loadout.Battle.Code);
                Civilian = WEquipment.FromCode(loadout.Civilian.Code, civilian: true);
                Alternates = [.. loadout.Alternates.Select(eq => WEquipment.FromCode(eq.Code))];
            }
            // Need to detect categories
            else
            {
                WEquipment battle = null;
                WEquipment civilian = null;

                foreach (var eq in loadout.Equipments)
                {
                    if (eq.IsCivilian && civilian == null)
                        civilian = WEquipment.FromCode(eq.Code, civilian: true);
                    else if (!eq.IsCivilian && battle == null)
                        battle = WEquipment.FromCode(eq.Code);

                    if (battle != null && civilian != null)
                        break;
                }

                // Replace the entire list so no alternates leak in from source
                Equipments =
                [
                    battle ?? WEquipment.FromCode(null),
                    civilian ?? WEquipment.FromCode(null, civilian: true),
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
                var horseItem = eq.GetItem(EquipmentIndex.Horse);
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
            var bestHorseOfParent = _owner.Parent?.Loadout.FindBestHorseCategory();

            Log.Info(
                $"Computed upgrade item requirement for {_owner.Name}: "
                    + $"bestHorse={bestHorse}, "
                    + $"bestHorseOfParent={bestHorseOfParent}"
            );

            if (IsBetterHorseCategory(bestHorse, bestHorseOfParent))
                return bestHorse;
            else
                return null;
        }
    }
}
