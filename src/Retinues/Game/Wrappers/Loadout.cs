using System.Collections.Generic;
using System.Linq;
using Retinues.Configuration;
using Retinues.Features.Equipments;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;
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

    /// <summary>
    /// Loadout is the structural model of all sets for a troop:
    /// contains/create/remove sets, slot contents, and all counting utilities.
    /// </summary>
    [SafeClass]
    public class WLoadout(WCharacter troop)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Troop                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WCharacter _troop = troop;
        public WCharacter Troop => _troop;

        private bool IsHero => _troop.IsHero;
        private Hero Hero => _troop.Base.HeroObject;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Equipment Lists                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Base equipment list.
        /// For troops: CharacterObject._equipmentRoster.AllEquipments.
        /// For heroes: [Hero.BattleEquipment, Hero.CivilianEquipment].
        /// </summary>
        public List<Equipment> BaseEquipments
        {
            get
            {
                if (IsHero)
                {
                    if (Hero == null)
                        return [];

                    var list = new List<Equipment>();
                    if (Hero.BattleEquipment != null)
                        list.Add(Hero.BattleEquipment);
                    if (Hero.CivilianEquipment != null)
                        list.Add(Hero.CivilianEquipment);
                    return list;
                }

                return Reflector
                        .GetFieldValue<MBEquipmentRoster>(_troop.Base, "_equipmentRoster")
                        ?.AllEquipments ?? [];
            }
            set
            {
                // For heroes, we do not replace the Equipment instances themselves.
                // All hero editing goes through SetEquipments / ApplyHeroEquipments.
                if (IsHero)
                    return;

                var roster = new MBEquipmentRoster();
                Reflector.SetFieldValue(roster, "_equipments", new MBList<Equipment>(value ?? []));
                Reflector.SetFieldValue(_troop.Base, "_equipmentRoster", roster);
            }
        }

        /// <summary>
        /// All equipment sets as wrapped objects.
        /// Troops: one WEquipment per roster entry.
        /// Heroes: index 0 = battle, index 1 = civilian (when present).
        /// </summary>
        public List<WEquipment> Equipments =>
            [.. BaseEquipments.Select(e => new WEquipment(e, this, BaseEquipments.IndexOf(e)))];

        /// <summary>
        /// Set the equipment sets from wrapped objects.
        /// For heroes, this copies into Hero.BattleEquipment / CivilianEquipment.
        /// For troops, this writes the CharacterObject._equipmentRoster.
        /// </summary>
        public void SetEquipments(List<WEquipment> value)
        {
            if (IsHero)
            {
                ApplyHeroEquipments(value);
                Troop.NeedsPersistence = true;
                return;
            }

            BaseEquipments = [.. value.Select(we => we.Base)];
            Troop.NeedsPersistence = true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Accessors                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WEquipment Battle =>
            IsHero ? Equipments.FirstOrDefault() : Equipments.FirstOrDefault(eq => !eq.IsCivilian);

        public WEquipment Civilian =>
            IsHero
                ? (Equipments.Count > 1 ? Equipments[1] : null)
                : Equipments.FirstOrDefault(eq => eq.IsCivilian);

        public List<WEquipment> BattleSets =>
            IsHero ? (Battle != null ? [Battle] : []) : [.. Equipments.Where(eq => !eq.IsCivilian)];

        public List<WEquipment> CivilianSets =>
            IsHero
                ? (Civilian != null ? [Civilian] : [])
                : [.. Equipments.Where(eq => eq.IsCivilian)];

        /// <summary>
        /// Get equipment set by index.
        /// </summary>
        public WEquipment Get(int index)
        {
            var list = Equipments;
            if (index < 0 || index >= list.Count)
                return null;
            return list[index];
        }

        /// <summary>
        /// Get equipment category by index.
        /// For heroes: 0 = battle, 1 = civilian.
        /// </summary>
        public EquipmentCategory GetCategory(int index)
        {
            if (IsHero)
            {
                if (index == 0)
                    return EquipmentCategory.Battle;
                if (index == 1)
                    return EquipmentCategory.Civilian;
                return EquipmentCategory.Invalid;
            }

            var eq = Get(index);
            if (eq == null)
                return EquipmentCategory.Invalid;
            return eq.IsCivilian ? EquipmentCategory.Civilian : EquipmentCategory.Battle;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Create / Remove Sets                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Create a new battle equipment set.
        /// </summary>
        public WEquipment CreateBattleSet() => CreateSet(civilian: false);

        /// <summary>
        /// Create a new civilian equipment set.
        /// </summary>
        public WEquipment CreateCivilianSet() => CreateSet(civilian: true);

        /// <summary>
        /// Create a new equipment set of given type.
        /// For heroes, returns the existing battle/civilian set (no extra sets).
        /// </summary>
        public WEquipment CreateSet(bool civilian)
        {
            if (IsHero)
                return civilian ? Civilian ?? Battle : Battle ?? Civilian;

            var we = WEquipment.FromCode(null, this, Equipments.Count, forceCivilian: civilian);
            var list = Equipments;
            list.Add(we);
            SetEquipments(list);
            Normalize();
            Troop.NeedsPersistence = true;
            return we;
        }

        /// <summary>
        /// Flip one set to civilian/battle while enforcing "keep >=1 of each".
        /// </summary>
        public void ToggleCivilian(WEquipment eq, bool makeCivilian)
        {
            if (eq == null)
                return;

            // Heroes only have one battle + one civilian; flipping categories makes no sense here.
            if (IsHero)
                return;

            if (makeCivilian && !eq.IsCivilian && BattleSets.Count <= 1)
                return;
            if (!makeCivilian && eq.IsCivilian && CivilianSets.Count <= 1)
                return;

            eq.SetCivilian(makeCivilian);
            Normalize();
            Troop.NeedsPersistence = true;
        }

        /// <summary>
        /// Remove a set while enforcing "keep >=1 battle and >=1 civilian".
        /// </summary>
        public void Remove(WEquipment equipment)
        {
            if (equipment == null)
                return;

            // Do not remove hero equipment sets; engine does not support "no battle set".
            if (IsHero)
                return;

            var list = Equipments;

            int idx = equipment.Index;
            if (idx < 0 || idx >= list.Count)
                return;

            bool removingCivilian = equipment.IsCivilian;
            int civilians = CivilianSets.Count;
            int battles = BattleSets.Count;

            if (removingCivilian && civilians <= 1)
                return;
            if (!removingCivilian && battles <= 1)
                return;

            list.RemoveAt(idx);
            SetEquipments(list);

            // Update combat-use mask indices (structure/indices maintenance)
            CombatEquipmentBehavior.OnRemoved(Troop, idx);

            Normalize();
            Troop.NeedsPersistence = true;
        }

        /// <summary>
        /// Reset to one empty battle + one empty civilian.
        /// </summary>
        public void Clear()
        {
            SetEquipments(
                [
                    WEquipment.FromCode(null, this, (int)EquipmentCategory.Battle, false),
                    WEquipment.FromCode(null, this, (int)EquipmentCategory.Civilian, true),
                ]
            );
        }

        /// <summary>
        /// Fill from another loadout (copy all sets or the first battle + civilian).
        /// For heroes, the relevant battle/civilian sets are copied into Hero.Battle/Civilian.
        /// </summary>
        public void FillFrom(WLoadout loadout, bool copyAll = false)
        {
            if (copyAll)
            {
                SetEquipments(
                    [
                        .. loadout.Equipments.Select(
                            (eq, i) => WEquipment.FromCode(eq.Code, this, i, eq.IsCivilian)
                        ),
                    ]
                );
                Normalize();
                return;
            }

            WEquipment battle = null,
                civilian = null;
            foreach (var eq in loadout.Equipments)
            {
                if (eq.IsCivilian && civilian == null)
                    civilian = WEquipment.FromCode(eq.Code, this, Equipments.Count, true);
                else if (!eq.IsCivilian && battle == null)
                    battle = WEquipment.FromCode(eq.Code, this, Equipments.Count, false);
                if (battle != null && civilian != null)
                    break;
            }

            SetEquipments(
                [
                    battle ?? WEquipment.FromCode(null, this, 0, false),
                    civilian ?? WEquipment.FromCode(null, this, 1, true),
                ]
            );

            Normalize();
        }

        /// <summary>
        /// Reorder so index 0 is a battle set; update derived upgrade requirement.
        /// </summary>
        public void Normalize()
        {
            // Derived (structure-based) recompute:
            Troop.UpgradeItemRequirement = ComputeUpgradeItemRequirement();

            if (Troop.IsCustom)
            {
                // Ensure at least one battle and one civilian set
                if (!BattleSets.Any())
                    CreateBattleSet();
                if (!CivilianSets.Any())
                    CreateCivilianSet();

                // Ensure first set is battle
                var list = Equipments;
                int firstBattle = list.FindIndex(e => !e.IsCivilian);
                if (firstBattle > 0)
                {
                    var battle = list[firstBattle];
                    list.RemoveAt(firstBattle);
                    list.Insert(0, battle);
                    SetEquipments(list);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Structure Apply                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Structure-only apply: set or unset the slot item in a given set.
        /// </summary>
        public void Apply(int setIndex, EquipmentIndex slot, WItem newItem)
        {
            var eq = Get(setIndex);
            if (eq == null)
                return;

            if (newItem == null)
                eq.UnsetItem(slot);
            else
                eq.SetItem(slot, newItem);

            // Update formation for battle sets; upgrade requirement for all sets.
            if (GetCategory(setIndex) == EquipmentCategory.Battle)
                Troop.FormationClass = eq.ComputeFormationClass();

            Troop.UpgradeItemRequirement = ComputeUpgradeItemRequirement();

            foreach (var upgrade in Troop.UpgradeTargets)
                upgrade.UpgradeItemRequirement = upgrade.Loadout.ComputeUpgradeItemRequirement();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Counting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Flat list of all non-null items across all sets.
        /// </summary>
        public List<WItem> Items =>
            [.. Equipments.SelectMany(eq => eq.Items).Where(i => i != null)];

        /// <summary>
        /// Per-set multiplicity of items for a given set.
        /// </summary>
        public Dictionary<WItem, int> ItemsInSet(int setIndex)
        {
            var map = new Dictionary<WItem, int>();
            var eq = Get(setIndex);
            if (eq == null)
                return map;

            foreach (var slot in WEquipment.Slots)
            {
                var it = eq.Get(slot);
                if (it == null)
                    continue;
                map.TryGetValue(it, out int c);
                map[it] = c + 1;
            }
            return map;
        }

        /// <summary>
        /// How many times is this exact item equipped across all sets.
        /// </summary>
        public int CountEquipped(WItem item)
        {
            if (item == null)
                return 0;
            int c = 0;
            foreach (var eq in Equipments)
            foreach (var slot in WEquipment.Slots)
                if (eq.Get(slot) == item)
                    c++;
            return c;
        }

        /// <summary>
        /// Is this item equipped in any set other than the given one/slot.
        /// </summary>
        public bool IsEquippedElsewhere(
            WItem item,
            int excludingSetIndex = -1,
            EquipmentIndex? excludingSlot = null
        )
        {
            if (item == null)
                return false;
            var eq = Get(excludingSetIndex);

            for (int i = 0; i < Equipments.Count; i++)
            {
                var e = Equipments[i];
                foreach (var slot in WEquipment.Slots)
                {
                    if (
                        i == excludingSetIndex
                        && excludingSlot.HasValue
                        && slot == excludingSlot.Value
                    )
                        continue;
                    var it = e.Get(slot);
                    if (it == item)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Count of an item in a single set.
        /// </summary>
        public int CountInSet(WItem item, int setIndex)
        {
            if (item == null)
                return 0;
            var eq = Get(setIndex);
            if (eq == null)
                return 0;

            int c = 0;
            foreach (var slot in WEquipment.Slots)
                if (eq.Get(slot) == item)
                    c++;
            return c;
        }

        /// <summary>
        /// Max number of copies required (max per single set).
        /// </summary>
        public int MaxCountPerSet(WItem item)
        {
            if (item == null)
                return 0;
            int max = 0;
            for (int i = 0; i < Equipments.Count; i++)
            {
                int c = CountInSet(item, i);
                if (c > max)
                    max = c;
            }
            return max;
        }

        /// <summary>
        /// Global "real" items today: item -> required copies (max per set).
        /// </summary>
        public Dictionary<WItem, int> RequiredCopies()
        {
            var result = new Dictionary<WItem, int>();
            for (int i = 0; i < Equipments.Count; i++)
            {
                var perSet = ItemsInSet(i);
                foreach (var kv in perSet)
                {
                    var item = kv.Key;
                    var cnt = kv.Value;
                    if (!result.TryGetValue(item, out int cur) || cnt > cur)
                        result[item] = cnt;
                }
            }
            return result;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        What-if Helpers                 //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Max count of 'item' across sets except one (used for what-if).
        /// </summary>
        public int MaxOverOtherSets(WItem item, int excludingSet)
        {
            if (item == null)
                return 0;
            int max = 0;
            for (int i = 0; i < Equipments.Count; i++)
            {
                if (i == excludingSet)
                    continue;
                int c = CountInSet(item, i);
                if (c > max)
                    max = c;
            }
            return max;
        }

        /// <summary>
        /// Required copies for a given item AFTER a hypothetical replace in (setIndex, slot -> newItem).
        /// </summary>
        public int RequiredAfterForItem(
            WItem item,
            int setIndex,
            EquipmentIndex slot,
            WItem newItem
        )
        {
            if (item == null)
                return 0;

            var eq = Get(setIndex);
            var current = eq?.Get(slot);

            // Compute this set's count after the hypothetical change
            int countThisSet = CountInSet(item, setIndex);
            if (current == item)
                countThisSet--; // removing old from this slot
            if (newItem == item)
                countThisSet++; // putting new into this slot

            int otherMax = MaxOverOtherSets(item, setIndex);
            return countThisSet > otherMax ? countThisSet : otherMax;
        }

        /// <summary>
        /// Preview deletion of a set: item -> (before, after, deltaRemove).
        /// Use to refund only items whose required copies drop when removing the set.
        /// </summary>
        public Dictionary<WItem, (int before, int after, int deltaRemove)> PreviewDeleteSet(
            int setIndex
        )
        {
            var result = new Dictionary<WItem, (int before, int after, int deltaRemove)>();

            // Consider all items that exist anywhere (global union)
            var union = new HashSet<WItem>(Items);

            foreach (var item in union)
            {
                int before = MaxCountPerSet(item);
                int after = MaxOverOtherSets(item, setIndex); // best remaining set without this one
                int deltaRemove = before > after ? (before - after) : 0;
                result[item] = (before, after, deltaRemove);
            }

            return result;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Skill / Derived                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Compute the skill requirement for a given skill across all sets.
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
        /// Get item (horse) requirement for upgrading to a troop using this loadout.
        /// </summary>
        public ItemCategory ComputeUpgradeItemRequirement()
        {
            if (!Troop.IsCustom && Config.KeepUpgradeRequirementsForVanilla)
                return Troop.UpgradeItemRequirement;

            var bestHorse = FindBestHorseCategory();
            var bestHorseOfParent = Troop.Parent?.Loadout.FindBestHorseCategory();

            if (Config.NeverRequireNobleHorse && bestHorse == DefaultItemCategories.NobleHorse)
                bestHorse = DefaultItemCategories.WarHorse;

            return IsBetterHorseCategory(bestHorse, bestHorseOfParent) ? bestHorse : null;
        }

        /// <summary>
        /// Get horse requirement for upgrading to a troop using this loadout.
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
        /// Find the best horse category across all sets.
        /// </summary>
        private ItemCategory FindBestHorseCategory()
        {
            ItemCategory bestCategory = null;
            foreach (var eq in Equipments)
            {
                if (eq.IsCivilian && Config.IgnoreCivilianHorseForUpgradeRequirements)
                    continue;

                var horseItem = eq.Get(EquipmentIndex.Horse);
                if (horseItem != null)
                {
                    var category = horseItem.Category;
                    if (bestCategory == null || IsBetterHorseCategory(category, bestCategory))
                        bestCategory = category;
                }
            }
            return bestCategory;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Hero helpers                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void ApplyHeroEquipments(List<WEquipment> sets)
        {
            if (!IsHero)
                return;

            var hero = Hero;
            if (hero == null || sets == null || sets.Count == 0)
                return;

            var battleSet = sets.FirstOrDefault(eq => !eq.IsCivilian) ?? sets.FirstOrDefault();
            var civilianSet = sets.FirstOrDefault(eq => eq.IsCivilian);

            if (battleSet != null)
                CopyEquipment(battleSet.Base, hero.BattleEquipment);
            if (civilianSet != null)
                CopyEquipment(civilianSet.Base, hero.CivilianEquipment);
        }

        private static void CopyEquipment(Equipment src, Equipment dst)
        {
            if (src == null || dst == null)
                return;

            for (var i = 0; i < Equipment.EquipmentSlotLength; i++)
                dst[i] = src[i];
        }
    }
}
