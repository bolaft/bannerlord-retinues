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

    public class MEquipmentRoster(MBEquipmentRoster @base, WCharacter owner)
        : MBase<MBEquipmentRoster>(@base)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Equipments                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        MAttribute<MBList<Equipment>> EquipmentsAttribute =>
            Attribute<MBList<Equipment>>("_equipments");

        MAttribute<List<MEquipment>> EquipmentsMAttribute =>
            Attribute<List<MEquipment>>(
                getter: _ =>
                    [
                        .. (EquipmentsAttribute.Get() ?? []).Select(
                            (e, i) => new MEquipment(e, owner)
                        ),
                    ],
                setter: (_, value) =>
                {
                    EquipmentsAttribute.Set([.. (value ?? []).Select(e => e.Base).ToList()]);
                }
            );

        public List<MEquipment> Equipments
        {
            get => EquipmentsMAttribute.Get();
            set
            {
                owner.TouchEquipments();
                EquipmentsMAttribute.Set(value);
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

            // Default: deep copy all sets
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
    }
}
