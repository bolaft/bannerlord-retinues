using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
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
            { // Shokuho compatibility: collapse to exactly 2 sets (Battle, Civilian)
                if (ModuleChecker.IsLoaded("Shokuho"))
                {
                    // Pick first non-civilian as Battle; if none, create an empty battle set
                    var battle =
                        value.FirstOrDefault(eq => !eq.IsCivilian)
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

            switch (index)
            {
                case 0:
                    return EquipmentCategory.Battle;
                case 1:
                    return EquipmentCategory.Civilian;
                default:
                    return EquipmentCategory.Alternate;
            }
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

        /// <summary>
        /// Removes an alternate equipment from this loadout.
        /// </summary>
        public void Remove(WEquipment equipment)
        {
            if (equipment.Index < (int)EquipmentCategory.Alternate)
                return; // cannot remove Battle/Civilian

            if (equipment.Index >= Equipments.Count)
                return; // invalid index

            var equipments = Equipments;
            equipments.RemoveAt(equipment.Index);
            Equipments = equipments; // re-assign to trigger any bindings
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
                if (eq.IsCivilian && Config.IgnoreCivilianHorseForUpgradeRequirements)
                    continue; // skip civilian equipments if configured to do so

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

            if (Config.NeverRequireNobleHorse && bestCategory == DefaultItemCategories.NobleHorse)
                return DefaultItemCategories.WarHorse; // downgrade to War Horse if configured

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

        /// <summary>
        /// Normalizes this loadout by ensuring the first two equipments are Battle and Civilian,
        /// and reordering alternates accordingly.
        /// </summary>
        public void Normalize()
        {
            var all = Equipments; // current order

            // Partition once
            var battles = new List<WEquipment>();
            var civilians = new List<WEquipment>();
            foreach (var eq in all)
                (eq.IsCivilian ? civilians : battles).Add(eq);

            // Pick canonical sets
            WEquipment battle = battles.Count > 0 ? battles[0] : null;
            WEquipment civilian = civilians.Count > 0 ? civilians[0] : null;

            // Drop extra civilians entirely
            // Alternates are non-civilian only
            var alternates = battles.Count > 1 ? battles.GetRange(1, battles.Count - 1) : [];

            // Create empties if missing
            battle ??= WEquipment.FromCode(null, this, 0); // battle
            civilian ??= WEquipment.FromCode(null, this, 1); // civilian

            // Rebuild ordered list: 0 = battle, 1 = civilian, 2+ = alternates (battle)
            var ordered = new List<WEquipment>(2 + alternates.Count)
            {
                // FromCode with the target index ensures type enforcement by your setter:
                WEquipment.FromCode(battle.Code, this, 0),
                WEquipment.FromCode(civilian.Code, this, 1),
            };

            for (int i = 0; i < alternates.Count; i++)
                ordered.Add(WEquipment.FromCode(alternates[i].Code, this, i + 2));

            // Assign back (Equipments/BaseEquipments setter enforces types by index:
            // index 1 -> civilian; all others -> battle)
            Equipments = ordered;

            // Refresh any downstream caches
            Troop.UpgradeItemRequirement = ComputeUpgradeItemRequirement();
        }
    }
}
