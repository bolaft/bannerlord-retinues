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
            get => GetBattleSets().FirstOrDefault() ?? CreateBattle();
            set
            {
                // Replace first battle set
                var list = Equipments;
                var i = list.FindIndex(e => !e.IsCivilian);
                if (i < 0)
                    list.Insert(0, value);
                else
                    list[i] = value;
                Equipments = list;
                Normalize();
            }
        }

        public WEquipment Civilian
        {
            get => GetCivilianSets().FirstOrDefault() ?? CreateCivilian();
            set
            {
                var list = Equipments;
                var i = list.FindIndex(e => e.IsCivilian);
                if (i < 0)
                    list.Add(value);
                else
                    list[i] = value;
                Equipments = list;
                Normalize();
            }
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
            {
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

        public IEnumerable<WEquipment> GetBattleSets() => Equipments.Where(eq => !eq.IsCivilian);

        public IEnumerable<WEquipment> GetCivilianSets() => Equipments.Where(eq => eq.IsCivilian);

        public WEquipment Get(int index)
        {
            if (index < 0 || index >= Equipments.Count)
                return null;
            return Equipments[index];
        }

        public EquipmentCategory GetCategory(int index)
        {
            var eq = Get(index);
            if (eq == null)
                return EquipmentCategory.Invalid;
            return eq.IsCivilian ? EquipmentCategory.Civilian : EquipmentCategory.Battle;
        }

        public WEquipment CreateBattle()
        {
            var we = WEquipment.FromCode(null, this, Equipments.Count, forceCivilian: false);
            var list = Equipments;
            list.Add(we);
            Equipments = list;
            Normalize();
            return we;
        }

        public WEquipment CreateCivilian()
        {
            var we = WEquipment.FromCode(null, this, Equipments.Count, forceCivilian: true);
            var list = Equipments;
            list.Add(we);
            Equipments = list;
            Normalize();
            return we;
        }

        /// <summary>
        /// Flip one set to civilian/battle and keep invariants.
        /// </summary>
        public void ToggleCivilian(WEquipment eq, bool makeCivilian)
        {
            if (eq == null)
                return;

            // If switching a battle set to civilian and it is the last battle set → forbid.
            if (makeCivilian && !eq.IsCivilian && GetBattleSets().Count() <= 1)
                return;

            // If switching a civilian set to battle and it is the last civilian set → forbid.
            if (!makeCivilian && eq.IsCivilian && GetCivilianSets().Count() <= 1)
                return;

            eq.SetCivilian(makeCivilian);

            // Still normalize (index 0 battle) but do NOT auto-create sets here anymore.
            Normalize();
        }

        /// <summary>
        /// Remove a set while enforcing: ≥1 battle AND ≥1 civilian must remain.
        /// </summary>
        public void Remove(WEquipment equipment)
        {
            var list = Equipments;
            if (equipment == null)
                return;
            var idx = equipment.Index;
            if (idx < 0 || idx >= list.Count)
                return;

            bool removingCivilian = equipment.IsCivilian;
            int civilians = GetCivilianSets().Count();
            int battles = GetBattleSets().Count();

            if (removingCivilian && civilians <= 1)
                return; // cannot remove last civilian
            if (!removingCivilian && battles <= 1)
                return; // cannot remove last battle

            list.RemoveAt(idx);
            Equipments = list;

            // Update combat-use mask indices
            Features.Loadouts.Behaviors.CombatLoadoutBehavior.OnRemoved(Troop, idx);

            Normalize();
        }

        /// <summary>
        /// Clears all equipments, setting them to default empty equipments.
        /// </summary>
        public void Clear()
        {
            Equipments =
            [
                WEquipment.FromCode(null, this, (int)EquipmentCategory.Battle, false),
                WEquipment.FromCode(null, this, (int)EquipmentCategory.Civilian, true),
            ];
        }

        /// <summary>
        /// Fill from another loadout.
        /// If copyAll==true, copy all sets as-is; otherwise copy first battle + first civilian only.
        /// </summary>
        public void FillFrom(WLoadout loadout, bool copyAll = false)
        {
            if (copyAll)
            {
                Equipments =
                [
                    .. loadout.Equipments.Select(
                        (eq, i) => WEquipment.FromCode(eq.Code, this, i, eq.IsCivilian)
                    ),
                ];
                EnsureMinimumSets();
                Normalize();
                return;
            }

            // old behavior: pick one battle and one civilian
            WEquipment battle = null;
            WEquipment civilian = null;

            foreach (var eq in loadout.Equipments)
            {
                if (eq.IsCivilian && civilian == null)
                    civilian = WEquipment.FromCode(
                        eq.Code,
                        this,
                        Equipments.Count,
                        forceCivilian: true
                    );
                else if (!eq.IsCivilian && battle == null)
                    battle = WEquipment.FromCode(
                        eq.Code,
                        this,
                        Equipments.Count,
                        forceCivilian: false
                    );

                if (battle != null && civilian != null)
                    break;
            }

            Equipments =
            [
                battle ?? WEquipment.FromCode(null, this, 0, forceCivilian: false),
                civilian ?? WEquipment.FromCode(null, this, 1, forceCivilian: true),
            ];

            Normalize();
        }

        /// <summary>
        /// Ensure there is at least one battle and one civilian set.
        /// </summary>
        public void EnsureMinimumSets()
        {
            if (!GetBattleSets().Any())
                CreateBattle();

            if (!GetCivilianSets().Any())
                CreateCivilian();
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
        /// Reorder so that index 0 is a battle set (if any), preserving relative order of the rest.
        /// </summary>
        public void Normalize()
        {
            var list = Equipments;
            if (list.Count == 0)
            {
                EnsureMinimumSets();
                list = Equipments;
            }

            // Move the first battle set to index 0 if necessary
            int firstBattle = list.FindIndex(e => !e.IsCivilian);
            if (firstBattle > 0)
            {
                var battle = list[firstBattle];
                list.RemoveAt(firstBattle);
                list.Insert(0, battle);
                Equipments = list; // reassign to refresh indices
            }

            // Recompute upgrade horse requirement (checks all sets)
            Troop.UpgradeItemRequirement = ComputeUpgradeItemRequirement();
        }
    }
}
