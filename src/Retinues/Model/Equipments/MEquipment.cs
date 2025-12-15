using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.Core;

namespace Retinues.Model.Equipments
{
    public class MEquipment(Equipment @base) : MBase<Equipment>(@base), IEquatable<MEquipment>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Owner                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Owner { get; set; }

        internal void NotifyChanged()
        {
            Owner?.OnEquipmentChanged();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Creation                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static MEquipment Create(bool civilian = false, MEquipment source = null)
        {
            var equipment =
                source == null ? new Equipment() : Equipment.CreateFromEquipmentCode(source.Code);

            if (equipment == null)
                equipment = new Equipment();

            var me = new MEquipment(equipment);
            me.EquipmentType = civilian
                ? Equipment.EquipmentType.Civilian
                : Equipment.EquipmentType.Battle;
            return me;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Main Properties                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Code => Base.CalculateEquipmentCode() ?? string.Empty;

        // Do NOT persist this via MAttribute. It's part of the serialized token.
        public Equipment.EquipmentType EquipmentType
        {
            get => Reflection.GetFieldValue<Equipment.EquipmentType>(Base, "_equipmentType");
            set => Reflection.SetFieldValue(Base, "_equipmentType", value);
        }

        public bool IsCivilian
        {
            get => EquipmentType == Equipment.EquipmentType.Civilian;
            set =>
                EquipmentType = value
                    ? Equipment.EquipmentType.Civilian
                    : Equipment.EquipmentType.Battle;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Items API                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WItem GetItem(EquipmentIndex index)
        {
            var element = Base[index];
            var item = element.Item;
            return item == null ? null : WItem.Get(item);
        }

        public void SetItem(EquipmentIndex index, WItem item)
        {
            var element = item == null ? EquipmentElement.Invalid : new EquipmentElement(item.Base);
            Base[index] = element;
            NotifyChanged();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //              Serialization (single equipment)           //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        const string KeyVersion = "v";
        const string KeyType = "type";
        const string KeyCode = "code";

        const string Version1 = "1";
        const string TypeBattle = "battle";
        const string TypeCivilian = "civilian";

        public string Serialize()
        {
            var kv = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [KeyVersion] = Version1,
                [KeyType] = IsCivilian ? TypeCivilian : TypeBattle,
                [KeyCode] = Code ?? string.Empty,
            };

            return SerializeKv(kv);
        }

        public static MEquipment Deserialize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (value.IndexOf('=') < 0)
                return null; // No legacy support by design.

            var map = DeserializeKv(value);

            map.TryGetValue(KeyType, out var t);
            map.TryGetValue(KeyCode, out var c);

            var eq = string.IsNullOrEmpty(c)
                ? new Equipment()
                : Equipment.CreateFromEquipmentCode(c);
            if (eq == null)
                eq = new Equipment();

            var me = new MEquipment(eq);
            me.EquipmentType = string.Equals(t, TypeCivilian, StringComparison.OrdinalIgnoreCase)
                ? Equipment.EquipmentType.Civilian
                : Equipment.EquipmentType.Battle;

            return me;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Serialization (list container)          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        const string KeyKind = "kind";
        const string KindList = "list";
        const string KeyCount = "n";
        const string KeyEntryPrefix = "e";

        public static string SerializeMany(IReadOnlyList<MEquipment> equipments)
        {
            if (equipments == null || equipments.Count == 0)
                return string.Empty;

            var kv = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [KeyVersion] = Version1,
                [KeyKind] = KindList,
                [KeyCount] = equipments.Count.ToString(),
            };

            for (int i = 0; i < equipments.Count; i++)
                kv[KeyEntryPrefix + i] = equipments[i]?.Serialize() ?? string.Empty;

            return SerializeKv(kv);
        }

        public static List<MEquipment> DeserializeMany(string value)
        {
            var result = new List<MEquipment>();

            if (string.IsNullOrEmpty(value))
                return result;

            if (value.IndexOf('=') < 0)
                return result; // No legacy support by design.

            var map = DeserializeKv(value);

            if (
                map.TryGetValue(KeyKind, out var kind)
                && !string.Equals(kind, KindList, StringComparison.OrdinalIgnoreCase)
            )
                return result;

            for (int i = 0; ; i++)
            {
                if (!map.TryGetValue(KeyEntryPrefix + i, out var token))
                    break;

                var me = Deserialize(token);
                if (me != null)
                    result.Add(me);
            }

            return result;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       KV helpers                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static string SerializeKv(Dictionary<string, string> map)
        {
            if (map == null || map.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            var first = true;

            foreach (var kv in map)
            {
                if (!first)
                    sb.Append('&');
                first = false;

                sb.Append(Uri.EscapeDataString(kv.Key ?? string.Empty));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(kv.Value ?? string.Empty));
            }

            return sb.ToString();
        }

        static Dictionary<string, string> DeserializeKv(string value)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            var parts = value.Split(['&'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var eq = part.IndexOf('=');
                if (eq <= 0)
                    continue;

                var k = Uri.UnescapeDataString(part.Substring(0, eq));
                var v = Uri.UnescapeDataString(part.Substring(eq + 1));

                if (string.IsNullOrEmpty(k))
                    continue;

                map[k] = v ?? string.Empty;
            }

            return map;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Equality                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public bool Equals(MEquipment other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return ReferenceEquals(Base, other.Base);
        }

        public override bool Equals(object obj) => obj is MEquipment other && Equals(other);

        public override int GetHashCode()
        {
            return Base == null ? 0 : RuntimeHelpers.GetHashCode(Base);
        }

        public static bool operator ==(MEquipment left, MEquipment right) => Equals(left, right);

        public static bool operator !=(MEquipment left, MEquipment right) => !Equals(left, right);
    }
}
