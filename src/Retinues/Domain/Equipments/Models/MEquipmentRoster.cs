using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Model;
using Retinues.Framework.Model.Attributes;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Domain.Equipments.Models
{
    public enum EquipmentCopyMode
    {
        All,
        FirstOfEach,
        Reset,
    }

    public class MEquipmentRoster(MBEquipmentRoster @base, WCharacter owner)
        : MBase<MBEquipmentRoster>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Equipments                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<MBList<Equipment>> EquipmentsAttribute =>
            Attribute<MBList<Equipment>>("_equipments");

        private List<MEquipment> _equipmentsCache;

        public List<MEquipment> Equipments
        {
            get
            {
                if (_equipmentsCache != null)
                    return _equipmentsCache;

                var list = EquipmentsAttribute.Get() ?? [];
                _equipmentsCache = [.. list.Select(e => new MEquipment(e, owner))];
                return _equipmentsCache;
            }
            set
            {
                _equipmentsCache = value ?? [];
                EquipmentsAttribute.Set([.. _equipmentsCache.Select(e => e.Base).ToList()]);

                owner.OnEquipmentChange();
            }
        }

        public void Add(MEquipment equipment)
        {
            var list = Equipments.ToList();
            list.Add(equipment);
            Equipments = list;
        }

        public void Remove(MEquipment equipment)
        {
            var list = Equipments.ToList();
            list.Remove(equipment);
            Equipments = list;

            // Update formation class in case the first battle equipment was removed.
            owner.UpdateFormationClass();
        }

        public void Copy(MEquipmentRoster source, EquipmentCopyMode mode = EquipmentCopyMode.All)
        {
            if (mode == EquipmentCopyMode.Reset)
            {
                Reset();
                return;
            }

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var sourceEquipments = source.Equipments ?? [];

            if (mode == EquipmentCopyMode.FirstOfEach)
            {
                MEquipment firstBattle = null;
                MEquipment firstCivil = null;

                for (int i = 0; i < sourceEquipments.Count; i++)
                {
                    var src = sourceEquipments[i];
                    if (src == null)
                        continue;

                    if (src.IsCivilian)
                    {
                        if (firstCivil == null)
                            firstCivil = src;
                    }
                    else
                    {
                        if (firstBattle == null)
                            firstBattle = src;
                    }

                    if (firstBattle != null && firstCivil != null)
                        break;
                }

                var newList = new List<MEquipment>(2)
                {
                    firstBattle != null
                        ? MEquipment.Create(owner, civilian: false, source: firstBattle)
                        : MEquipment.Create(owner, civilian: false, source: null),
                    firstCivil != null
                        ? MEquipment.Create(owner, civilian: true, source: firstCivil)
                        : MEquipment.Create(owner, civilian: true, source: null),
                };

                Equipments = newList;
                return;
            }

            var all = new List<MEquipment>();
            for (int i = 0; i < sourceEquipments.Count; i++)
            {
                var src = sourceEquipments[i];

                var clone =
                    src == null
                        ? MEquipment.Create(owner, civilian: false, source: null)
                        : MEquipment.Create(owner, src.IsCivilian, src);

                all.Add(clone);
            }

            Equipments = all;
        }

        public void Reset()
        {
            Equipments = [];

#if BL13
            Add(new MEquipment(new Equipment(Equipment.EquipmentType.Battle), owner));
            Add(new MEquipment(new Equipment(Equipment.EquipmentType.Civilian), owner));
#else
            Add(new MEquipment(new Equipment(false), owner));
            Add(new MEquipment(new Equipment(true), owner));
#endif
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Items                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public IEnumerable<WItem> Items
        {
            get
            {
                foreach (MEquipment equipment in Equipments)
                {
                    foreach (WItem item in equipment.Items)
                    {
                        yield return item;
                    }
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Cached Item Counts                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly Cache<MEquipmentRoster, Dictionary<string, int>> _itemCountsCache = new(
            r => r.ComputeItemCounts()
        );

        /// <summary>
        /// Required roster stock per item id.
        /// Rule: for each item, keep the max number of copies used by any single equipment.
        /// </summary>
        public Dictionary<string, int> ItemCountsById => _itemCountsCache.Get(this);

        public void InvalidateItemCountsCache() => _itemCountsCache.Clear();

        private Dictionary<string, int> ComputeItemCounts()
        {
            Dictionary<string, int> result = [];

            // Use cached wrappers, not fresh wrappers
            var list = Equipments;

            for (int i = 0; i < list.Count; i++)
            {
                var me = list[i];
                if (me == null)
                    continue;

                Dictionary<string, int> per = [];

                foreach (var item in me.Items)
                {
                    if (item == null)
                        continue;

                    string id = item.StringId;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    if (!per.ContainsKey(id))
                        per[id] = 0;

                    per[id]++;
                }

                foreach (var kv in per)
                {
                    if (!result.TryGetValue(kv.Key, out int current))
                        result[kv.Key] = kv.Value;
                    else
                        result[kv.Key] = Math.Max(current, kv.Value);
                }
            }

            return result;
        }

        internal int GetMaxCountExcludingEquipment(Equipment exclude, string itemId)
        {
            if (exclude == null || string.IsNullOrEmpty(itemId))
                return 0;

            int max = 0;

            var list = Equipments;
            for (int i = 0; i < list.Count; i++)
            {
                var me = list[i];
                if (me == null)
                    continue;

                if (ReferenceEquals(me.Base, exclude))
                    continue;

                int count = 0;
                foreach (var item in me.Items)
                {
                    if (item != null && item.StringId == itemId)
                        count++;
                }

                if (count > max)
                    max = count;
            }

            return max;
        }
    }
}
