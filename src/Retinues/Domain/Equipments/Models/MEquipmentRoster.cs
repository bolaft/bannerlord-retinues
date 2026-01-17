using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Equipments.Wrappers;
using Retinues.Framework.Model;
using Retinues.Framework.Model.Attributes;
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

    public partial class MEquipmentRoster(MBEquipmentRoster @base, WCharacter owner)
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

        /// <summary>
        /// Adds a new equipment to the roster.
        /// </summary>
        public void Add(MEquipment equipment)
        {
            var list = Equipments.ToList();
            list.Add(equipment);
            Equipments = list;
        }

        /// <summary>
        /// Removes an equipment from the roster.
        /// </summary>
        public void Remove(MEquipment equipment)
        {
            var list = Equipments.ToList();
            list.Remove(equipment);
            Equipments = list;

            // Update formation class in case the first battle equipment was removed.
            owner.UpdateFormationClass();
        }

        /// <summary>
        /// Copies equipments from another roster according to the specified mode.
        /// </summary>
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

        /// <summary>
        /// Resets the roster to default equipments.
        /// </summary>
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
    }
}
