using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Retinues.Utilities;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model
{
    // Keep numeric values stable for logs / ordering.
    public enum MPersistencePriority
    {
        High = 0,
        Normal = 500,
        Low = 750,
    }

    internal static class MPersistence
    {
        private static readonly object Sync = new();

        private static Dictionary<string, string> _store = new(StringComparer.Ordinal);

        private static readonly Dictionary<string, IPersistentAttribute> _registered = new(
            StringComparer.Ordinal
        );

        private static readonly HashSet<string> _dirtyAttributeKeys = new(StringComparer.Ordinal);
        private static readonly HashSet<string> _dirtyOwnerKeys = new(StringComparer.Ordinal);

        private static bool _eagerApplied;
        private static readonly HashSet<string> _appliedKeys = new(StringComparer.Ordinal);

        private static int _applyDepth;
        public static bool IsApplying { get; private set; }

        // pv2|prioInt|serializerType|valueType|payload
        private const string EnvelopePrefix = "pv2|";

        // Wrapper value payloads (for attributes whose VALUE is a wrapper / list of wrappers)
        private const string WrapPrefix = "w1|";
        private const string WrapListPrefix = "wl1|";

        [StaticClearAction]
        public static void ClearAll()
        {
            lock (Sync)
            {
                _store.Clear();
                _registered.Clear();
                _dirtyAttributeKeys.Clear();
                _dirtyOwnerKeys.Clear();

                _appliedKeys.Clear();
                _eagerApplied = false;

                _applyDepth = 0;
                IsApplying = false;
            }
        }

        public static void AttachStore(Dictionary<string, string> store)
        {
            lock (Sync)
            {
                _store = store ?? new Dictionary<string, string>(StringComparer.Ordinal);
                Log.Info($"Persist.AttachStore: {_store.Count} entries.");
            }
        }

        public static Dictionary<string, string> GetStore()
        {
            lock (Sync)
                return _store;
        }

        public static IDisposable BeginApplying()
        {
            lock (Sync)
            {
                _applyDepth++;
                IsApplying = true;
            }

            return new ApplyScope();
        }

        private sealed class ApplyScope : IDisposable
        {
            public void Dispose()
            {
                lock (Sync)
                {
                    if (_applyDepth > 0)
                        _applyDepth--;

                    if (_applyDepth == 0)
                        IsApplying = false;
                }
            }
        }

        public static void Register(IPersistentAttribute attr)
        {
            if (attr == null || string.IsNullOrEmpty(attr.AttributeKey))
                return;

            lock (Sync)
            {
                _registered[attr.AttributeKey] = attr;

                // Do NOT apply here before eager pass (priority ordering).
                if (!_eagerApplied)
                    return;

                // Late-registration catch-up should NOT re-apply keys already applied by eager pass.
                if (_appliedKeys.Contains(attr.AttributeKey))
                    return;

                if (!_store.TryGetValue(attr.AttributeKey, out var raw))
                    return;

                if (
                    !TryUnpack(
                        raw,
                        out var prio,
                        out var serializerType,
                        out var valueType,
                        out var payload
                    )
                )
                {
                    Log.Warn(
                        $"Persist.Register: invalid envelope for '{attr.AttributeKey}', skipping apply."
                    );
                    return;
                }

                if (!CanApplyType(attr, valueType))
                {
                    Log.Warn(
                        $"Persist.Register: type mismatch for {attr.AttributeKey}. store={valueType ?? "-"}, attr={attr.ValueTypeName ?? "-"}; skipping."
                    );
                    return;
                }

                if (
                    !TryApply_NoThrow(
                        attr,
                        payload,
                        $"LateRegister(prio={prio}, ser={serializerType ?? "-"}, type={valueType ?? "-"})"
                    )
                )
                    return;

                _appliedKeys.Add(attr.AttributeKey);
                attr.ClearDirty();
            }
        }

        private static bool CanApplyType(IPersistentAttribute attr, string storedValueTypeName)
        {
            if (attr == null)
                return false;

            // If old data didn't store type, allow.
            if (string.IsNullOrEmpty(storedValueTypeName))
                return true;

            return string.Equals(storedValueTypeName, attr.ValueTypeName, StringComparison.Ordinal);
        }

        public static void MarkDirty(IPersistentAttribute attr)
        {
            if (attr == null || string.IsNullOrEmpty(attr.AttributeKey))
                return;

            // Important: avoid cascading dirties while we are applying persisted values.
            // (Some setters touch other attributes as part of internal consistency).
            if (IsApplying)
                return;

            lock (Sync)
            {
                _dirtyAttributeKeys.Add(attr.AttributeKey);

                if (!string.IsNullOrEmpty(attr.OwnerKey))
                    _dirtyOwnerKeys.Add(attr.OwnerKey);
            }
        }

        public static bool IsOwnerDirty(string ownerKey)
        {
            if (string.IsNullOrEmpty(ownerKey))
                return false;

            lock (Sync)
                return _dirtyOwnerKeys.Contains(ownerKey);
        }

        public static void FlushDirty()
        {
            List<IPersistentAttribute> dirty;
            lock (Sync)
            {
                if (_dirtyAttributeKeys.Count == 0)
                    return;

                dirty = new List<IPersistentAttribute>(_dirtyAttributeKeys.Count);

                foreach (var key in _dirtyAttributeKeys)
                {
                    if (_registered.TryGetValue(key, out var attr))
                        dirty.Add(attr);
                }

                _dirtyAttributeKeys.Clear();
                _dirtyOwnerKeys.Clear();
            }

            foreach (var attr in dirty)
            {
                try
                {
                    var payload = attr.Serialize();
                    var raw = Pack(
                        attr.Priority,
                        attr.SerializerTypeName,
                        attr.ValueTypeName,
                        payload
                    );

                    lock (Sync)
                        _store[attr.AttributeKey] = raw;

                    Log.Info(
                        $"Persist.Save: {attr.AttributeKey} (prio={(int)attr.Priority}, ser={attr.SerializerTypeName ?? "-"}, type={attr.ValueTypeName ?? "-"}) = {payload}"
                    );

                    attr.ClearDirty();
                }
                catch (Exception e)
                {
                    Log.Exception(e, $"Persist.Save failed: {attr.AttributeKey}");
                }
            }
        }

        /// <summary>
        /// Applies all persisted values eagerly by:
        /// 1) Parsing key as: <WrapperTypeFullName>:<StringId>:<AttributePropertyName>
        /// 2) Calling WrapperType.Get(stringId)
        /// 3) Fetching wrapper property/field named <AttributePropertyName> that is an IPersistentAttribute
        /// 4) Calling ApplySerialized(payload) on it (so the attribute setter runs)
        /// </summary>
        public static void ApplyLoadedEager()
        {
            using (BeginApplying())
            {
                List<PersistedEntry> entries;
                lock (Sync)
                {
                    entries = new List<PersistedEntry>(_store.Count);

                    foreach (var kv in _store)
                    {
                        if (
                            !TryParseKey(
                                kv.Key,
                                out var wrapperTypeName,
                                out var stringId,
                                out var attrName
                            )
                        )
                            continue;

                        if (
                            !TryUnpack(
                                kv.Value,
                                out var prio,
                                out var serializerType,
                                out var valueType,
                                out var payload
                            )
                        )
                        {
                            Log.Warn(
                                $"Persist.ApplyEager: invalid envelope for '{kv.Key}', skipping."
                            );
                            continue;
                        }

                        entries.Add(
                            new PersistedEntry
                            {
                                Key = kv.Key,
                                WrapperTypeName = wrapperTypeName,
                                StringId = stringId,
                                AttributeName = attrName,
                                Priority = prio,
                                SerializerTypeName = serializerType,
                                ValueTypeName = valueType,
                                Payload = payload,
                            }
                        );
                    }
                }

                entries.Sort(
                    (a, b) =>
                    {
                        var c = a.Priority.CompareTo(b.Priority);
                        if (c != 0)
                            return c;
                        return string.CompareOrdinal(a.Key, b.Key);
                    }
                );

                Log.Info($"Persist.ApplyEager: applying {entries.Count} entries...");

                foreach (var e in entries)
                {
                    try
                    {
                        ApplyEntryEager(e);
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex, $"Persist.ApplyEager failed: {e.Key}");
                    }
                }

                lock (Sync)
                    _eagerApplied = true;
            }
        }

        /// <summary>
        /// Applies values to already registered attributes (lazy safety net).
        /// </summary>
        public static void ApplyLoadedRegistered()
        {
            List<(IPersistentAttribute attr, string raw)> pairs;

            lock (Sync)
            {
                pairs = new List<(IPersistentAttribute, string)>(_registered.Count);

                foreach (var kv in _registered)
                {
                    if (_store.TryGetValue(kv.Key, out var raw))
                        pairs.Add((kv.Value, raw));
                }
            }

            using (BeginApplying())
            {
                foreach (var (attr, raw) in pairs)
                {
                    if (
                        !TryUnpack(
                            raw,
                            out var prio,
                            out var serializerType,
                            out var valueType,
                            out var payload
                        )
                    )
                    {
                        Log.Warn(
                            $"Persist.Apply(Lazy): invalid envelope for '{attr.AttributeKey}', skipping."
                        );
                        continue;
                    }

                    if (!CanApplyType(attr, valueType))
                    {
                        Log.Warn(
                            $"Persist.Apply(Lazy): type mismatch for {attr.AttributeKey}. store={valueType ?? "-"}, attr={attr.ValueTypeName ?? "-"}; skipping."
                        );
                        continue;
                    }

                    TryApply_NoThrow(
                        attr,
                        payload,
                        $"Lazy(prio={prio}, ser={serializerType ?? "-"}, type={valueType ?? "-"})"
                    );
                    attr.ClearDirty();
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Eager apply internals
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private struct PersistedEntry
        {
            public string Key;
            public string WrapperTypeName;
            public string StringId;
            public string AttributeName;
            public int Priority;
            public string SerializerTypeName;
            public string ValueTypeName;
            public string Payload;
        }

        private static void ApplyEntryEager(PersistedEntry e)
        {
            var wrapperType = ResolveType(e.WrapperTypeName);
            if (wrapperType == null)
            {
                Log.Warn(
                    $"Persist.ApplyEager: wrapper type not found: {e.WrapperTypeName} ({e.Key})"
                );
                return;
            }

            var wrapper = InvokeWrapperGet(wrapperType, e.StringId);
            if (wrapper == null)
            {
                Log.Warn(
                    $"Persist.ApplyEager: wrapper.Get failed: {e.WrapperTypeName}.Get('{e.StringId}') ({e.Key})"
                );
                return;
            }

            var attr = GetPersistentAttributeOnWrapper(wrapper, e.AttributeName);
            if (attr == null)
            {
                Log.Warn(
                    $"Persist.ApplyEager: wrapper attribute not found: {e.WrapperTypeName}:{e.StringId}:{e.AttributeName}"
                );
                return;
            }

            if (!string.Equals(attr.AttributeKey, e.Key, StringComparison.Ordinal))
            {
                Log.Warn(
                    $"Persist.ApplyEager: key mismatch. store='{e.Key}', attr='{attr.AttributeKey}'. Applying anyway."
                );
            }

            if (!CanApplyType(attr, e.ValueTypeName))
            {
                Log.Warn(
                    $"Persist.ApplyEager: type mismatch for {e.Key}. store={e.ValueTypeName ?? "-"}, attr={attr.ValueTypeName ?? "-"}; skipping."
                );
                return;
            }

            lock (Sync)
            {
                if (_appliedKeys.Contains(e.Key))
                    return;
            }

            if (
                !TryApply_NoThrow(
                    attr,
                    e.Payload,
                    $"Eager(prio={e.Priority}, ser={e.SerializerTypeName ?? "-"}, type={e.ValueTypeName ?? "-"})"
                )
            )
                return;

            lock (Sync)
                _appliedKeys.Add(e.Key);

            attr.ClearDirty();
        }

        private static bool TryApply_NoThrow(
            IPersistentAttribute attr,
            string payload,
            string context
        )
        {
            try
            {
                using (BeginApplying())
                {
                    attr.ApplySerialized(payload);
                }

                Log.Info($"Persist.Apply: {attr.AttributeKey} [{context}] = {payload}");
                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Persist.Apply failed: {attr.AttributeKey} [{context}]");
                return false;
            }
        }

        private static bool TryParseKey(
            string key,
            out string wrapperTypeName,
            out string stringId,
            out string attributeName
        )
        {
            wrapperTypeName = null;
            stringId = null;
            attributeName = null;

            if (string.IsNullOrEmpty(key))
                return false;

            // <WrapperTypeFullName>:<StringId>:<AttributePropertyName>
            var parts = key.Split(new[] { ':' }, 3);
            if (parts.Length != 3)
                return false;

            wrapperTypeName = parts[0];
            stringId = parts[1];
            attributeName = parts[2];
            return true;
        }

        private static IPersistentAttribute GetPersistentAttributeOnWrapper(
            object wrapper,
            string attributeName
        )
        {
            if (wrapper == null || string.IsNullOrEmpty(attributeName))
                return null;

            var t = wrapper.GetType();

            const BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.FlattenHierarchy;

            try
            {
                var p = t.GetProperty(attributeName, flags);
                if (p != null)
                    return p.GetValue(wrapper) as IPersistentAttribute;
            }
            catch
            {
                // ignore
            }

            try
            {
                var f = t.GetField(attributeName, flags);
                if (f != null)
                    return f.GetValue(wrapper) as IPersistentAttribute;
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static object InvokeWrapperGet(Type wrapperType, string stringId)
        {
            if (wrapperType == null || string.IsNullOrEmpty(stringId))
                return null;

            const BindingFlags flags =
                BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.FlattenHierarchy;

            var m = wrapperType.GetMethod("Get", flags, null, new[] { typeof(string) }, null);
            if (m == null)
                return null;

            try
            {
                return m.Invoke(null, new object[] { stringId });
            }
            catch
            {
                return null;
            }
        }

        private static Type ResolveType(string fullNameOrAqn)
        {
            if (string.IsNullOrEmpty(fullNameOrAqn))
                return null;

            var t = Type.GetType(fullNameOrAqn, throwOnError: false);
            if (t != null)
                return t;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(fullNameOrAqn, throwOnError: false);
                    if (t != null)
                        return t;
                }
                catch
                {
                    // ignore
                }
            }

            return null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Envelope
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private static string Pack(
            MPersistencePriority prio,
            string serializerTypeName,
            string valueTypeName,
            string payload
        )
        {
            return string.Concat(
                EnvelopePrefix,
                (int)prio,
                "|",
                Escape(serializerTypeName ?? ""),
                "|",
                Escape(valueTypeName ?? ""),
                "|",
                Escape(payload ?? "")
            );
        }

        private static bool TryUnpack(
            string raw,
            out int prio,
            out string serializerTypeName,
            out string valueTypeName,
            out string payload
        )
        {
            prio = (int)MPersistencePriority.Normal;
            serializerTypeName = null;
            valueTypeName = null;
            payload = null;

            if (
                string.IsNullOrEmpty(raw)
                || !raw.StartsWith(EnvelopePrefix, StringComparison.Ordinal)
            )
                return false;

            var rest = raw.Substring(EnvelopePrefix.Length);
            var split = rest.Split(new[] { '|' }, 4); // prio|ser|type|payload

            if (split.Length != 4)
                return false;

            if (
                !int.TryParse(
                    split[0],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out prio
                )
            )
                prio = (int)MPersistencePriority.Normal;

            serializerTypeName = Unescape(split[1]);
            if (string.IsNullOrEmpty(serializerTypeName))
                serializerTypeName = null;

            valueTypeName = Unescape(split[2]);
            if (string.IsNullOrEmpty(valueTypeName))
                valueTypeName = null;

            payload = Unescape(split[3]);
            return true;
        }

        private static string Escape(string s) => s == null ? "" : Uri.EscapeDataString(s);

        private static string Unescape(string s) =>
            string.IsNullOrEmpty(s) ? "" : Uri.UnescapeDataString(s);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Default value serializers (used by MAttribute when no custom serializer is provided)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        public static string SerializeValue(object value, Type type)
        {
            if (value == null)
                return null;

            // Wrapper single value
            if (IsWrapperType(type))
            {
                var id = GetWrapperStringId(value);
                var tName = value.GetType().FullName; // store runtime wrapper type
                return $"{WrapPrefix}{tName}|{Escape(id)}";
            }

            // Wrapper list value
            if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
            {
                var elemType = GetEnumerableElementType(type);
                if (elemType == null && value is IEnumerable en)
                {
                    foreach (var item in en)
                    {
                        if (item == null)
                            continue;
                        var it = item.GetType();
                        if (IsWrapperType(it))
                        {
                            elemType = it;
                            break;
                        }
                    }
                }

                if (elemType != null && IsWrapperType(elemType))
                {
                    var ids = new List<string>();
                    foreach (var item in (IEnumerable)value)
                    {
                        if (item == null)
                            continue;
                        var id = GetWrapperStringId(item);
                        if (id != null)
                            ids.Add(Escape(id));
                    }

                    return $"{WrapListPrefix}{elemType.FullName}|{string.Join(",", ids)}";
                }
            }

            if (type == typeof(string))
                return (string)value;

            if (type == typeof(TextObject))
                return value.ToString();

            if (type.IsEnum)
                return Convert
                    .ToInt32(value, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture);

            if (type == typeof(bool))
                return (bool)value ? "1" : "0";

            if (
                type == typeof(int)
                || type == typeof(long)
                || type == typeof(short)
                || type == typeof(uint)
                || type == typeof(ulong)
                || type == typeof(ushort)
                || type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal)
            )
                return Convert.ToString(value, CultureInfo.InvariantCulture);

            if (typeof(MBObjectBase).IsAssignableFrom(type))
                return ((MBObjectBase)value).StringId;

            throw new InvalidOperationException(
                $"No default serializer for type '{type.FullName}'."
            );
        }

        public static object DeserializeValue(string serialized, Type type)
        {
            if (serialized == null)
                return null;

            // Wrapper single value
            if (serialized.StartsWith(WrapPrefix, StringComparison.Ordinal))
            {
                var payload = serialized.Substring(WrapPrefix.Length);
                var split = payload.Split(new[] { '|' }, 2);

                var typeName = split.Length > 0 ? split[0] : null;
                var id = split.Length > 1 ? Unescape(split[1]) : null;

                var wrapperType = ResolveType(typeName) ?? type;
                if (!IsWrapperType(wrapperType))
                    return null;

                return InvokeWrapperGet(wrapperType, id);
            }

            // Wrapper list value
            if (serialized.StartsWith(WrapListPrefix, StringComparison.Ordinal))
            {
                var payload = serialized.Substring(WrapListPrefix.Length);
                var split = payload.Split(new[] { '|' }, 2);

                var elemTypeName = split.Length > 0 ? split[0] : null;
                var idsPart = split.Length > 1 ? split[1] : null;

                var declaredElem = GetEnumerableElementType(type);
                var elemType = ResolveType(elemTypeName) ?? declaredElem;

                if (!IsWrapperType(elemType))
                    return null;

                var ids = string.IsNullOrEmpty(idsPart)
                    ? Array.Empty<string>()
                    : idsPart.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                var listType = typeof(List<>).MakeGenericType(elemType);
                var list = (IList)Activator.CreateInstance(listType);

                for (int i = 0; i < ids.Length; i++)
                {
                    var id = Unescape(ids[i]);
                    var obj = InvokeWrapperGet(elemType, id);
                    if (obj != null)
                        list.Add(obj);
                }

                if (type.IsArray)
                {
                    var arr = Array.CreateInstance(elemType, list.Count);
                    list.CopyTo(arr, 0);
                    return arr;
                }

                return list;
            }

            if (type == typeof(string))
                return serialized;

            if (type == typeof(TextObject))
                return new TextObject(serialized);

            if (type.IsEnum)
            {
                var i = int.Parse(serialized, CultureInfo.InvariantCulture);
                return Enum.ToObject(type, i);
            }

            if (type == typeof(bool))
                return serialized == "1"
                    || string.Equals(serialized, "true", StringComparison.OrdinalIgnoreCase);

            if (type == typeof(int))
                return int.Parse(serialized, CultureInfo.InvariantCulture);
            if (type == typeof(long))
                return long.Parse(serialized, CultureInfo.InvariantCulture);
            if (type == typeof(short))
                return short.Parse(serialized, CultureInfo.InvariantCulture);
            if (type == typeof(uint))
                return uint.Parse(serialized, CultureInfo.InvariantCulture);
            if (type == typeof(ulong))
                return ulong.Parse(serialized, CultureInfo.InvariantCulture);
            if (type == typeof(ushort))
                return ushort.Parse(serialized, CultureInfo.InvariantCulture);
            if (type == typeof(byte))
                return byte.Parse(serialized, CultureInfo.InvariantCulture);
            if (type == typeof(sbyte))
                return sbyte.Parse(serialized, CultureInfo.InvariantCulture);
            if (type == typeof(float))
                return float.Parse(serialized, CultureInfo.InvariantCulture);
            if (type == typeof(double))
                return double.Parse(serialized, CultureInfo.InvariantCulture);
            if (type == typeof(decimal))
                return decimal.Parse(serialized, CultureInfo.InvariantCulture);

            if (typeof(MBObjectBase).IsAssignableFrom(type))
            {
                var mgr = MBObjectManager.Instance;
                if (mgr == null)
                    return null;

                // Pick exact: T GetObject<T>(string)
                var mi = typeof(MBObjectManager)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m =>
                        m.Name == "GetObject"
                        && m.IsGenericMethodDefinition
                        && m.GetGenericArguments().Length == 1
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(string)
                    );

                if (mi == null)
                    return null;

                var g = mi.MakeGenericMethod(type);
                return g.Invoke(mgr, new object[] { serialized });
            }

            throw new InvalidOperationException(
                $"No default deserializer for type '{type.FullName}'."
            );
        }

        // Wrapper type detection helpers (for wrapper VALUE serialization, not for persistence keys)
        private static bool IsWrapperType(Type t)
        {
            if (t == null)
                return false;

            var cur = t;
            while (cur != null && cur != typeof(object))
            {
                if (cur.IsGenericType && cur.GetGenericTypeDefinition() == typeof(WBase<,>))
                    return true;

                cur = cur.BaseType;
            }

            return false;
        }

        private static Type GetEnumerableElementType(Type enumerableType)
        {
            if (enumerableType == null)
                return null;

            if (enumerableType.IsArray)
                return enumerableType.GetElementType();

            if (enumerableType.IsGenericType)
                return enumerableType.GetGenericArguments().FirstOrDefault();

            return null;
        }

        private static string GetWrapperStringId(object wrapper)
        {
            if (wrapper == null)
                return null;

            try
            {
                return (string)wrapper.GetType().GetProperty("StringId")?.GetValue(wrapper);
            }
            catch
            {
                return null;
            }
        }
    }
}
