using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Retinues.Model.Equipments
{
    public class MEquipmentRoster : MBase<MBEquipmentRoster>
    {
        readonly WCharacter _owner;

        public MEquipmentRoster(MBEquipmentRoster @base, WCharacter owner)
            : base(@base)
        {
            _owner = owner;
            EnsureSerializableAttributes();
        }

        void EnsureSerializableAttributes()
        {
            _ = EquipmentsSerializedAttribute;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Equipments                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        // Raw backing list on MBEquipmentRoster.
        MAttribute<MBList<Equipment>> EquipmentsAttribute =>
            Attribute<MBList<Equipment>>("_equipments");

        public List<MEquipment> Equipments
        {
            get => [.. (EquipmentsAttribute.Get() ?? []).Select(e => new MEquipment(e, _owner))];
            set => EquipmentsAttribute.Set([.. (value ?? []).Select(e => e.Base).ToList()]);
        }

        // This is what makes MBase.Serialize() non-empty for equipment rosters.
        // Format: "B|<escapedCode>;C|<escapedCode>;..."
        MAttribute<string> EquipmentsSerializedAttribute =>
            Attribute(
                getter: _ => SerializeEquipments(),
                setter: (_, s) => DeserializeEquipments(s),
                serializable: true,
                targetName: nameof(Equipments)
            );

        string SerializeEquipments()
        {
            var list = EquipmentsAttribute.Get();
            if (list == null || list.Count == 0)
                return string.Empty;

            var parts = new List<string>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var me = new MEquipment(list[i], _owner);

                var kind = me.IsCivilian ? "C" : "B";
                var code = me.Code ?? string.Empty;

                parts.Add($"{kind}|{Uri.EscapeDataString(code)}");
            }

            return string.Join(";", parts);
        }

        void DeserializeEquipments(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
            {
                // Treat empty as "no custom roster persisted".
                // You can choose Reset() or just leave as-is; Reset() is safer for editor expectations.
                Reset();
                return;
            }

            var outList = new List<MEquipment>();

            var entries = serialized.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var parts = entry.Split(new[] { '|' }, 2);
                if (parts.Length != 2)
                    continue;

                var kind = parts[0];
                var code = Uri.UnescapeDataString(parts[1] ?? string.Empty);

                var eq = !string.IsNullOrEmpty(code)
                    ? Equipment.CreateFromEquipmentCode(code)
                    : null;
                eq ??= new Equipment();

#if BL13
                // Avoid calling MEquipment.EquipmentType setter here (it touches owner -> dirties again while applying).
                var t =
                    kind == "C" ? Equipment.EquipmentType.Civilian : Equipment.EquipmentType.Battle;
                Reflection.SetFieldValue(eq, "_equipmentType", t);
#endif

                outList.Add(new MEquipment(eq, _owner));
            }

            Equipments = outList;

            if (outList.Count == 0)
                Reset();
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

        public void Copy(MEquipmentRoster source)
        {
            Equipments = [.. source.Equipments];
        }

        public void Reset()
        {
            Equipments = [];

#if BL13
            Add(new MEquipment(new Equipment(Equipment.EquipmentType.Battle), _owner));
            Add(new MEquipment(new Equipment(Equipment.EquipmentType.Civilian), _owner));
#else
            Add(new MEquipment(new Equipment(false), _owner));
            Add(new MEquipment(new Equipment(true), _owner));
#endif
        }
    }
}
