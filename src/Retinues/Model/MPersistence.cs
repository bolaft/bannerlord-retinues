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
    /// <summary>
    /// Non-generic interface so persistence can track attributes without knowing T.
    /// </summary>
    internal interface IPersistentAttribute
    {
        string OwnerKey { get; }
        string AttributeKey { get; }
        bool IsDirty { get; }

        string Serialize();
        void ApplySerialized(string serialized);

        void ClearDirty();
    }

    public enum MPersistencePriority
    {
        High = 0,
        Normal = 100,
        Low = 200,
    }

    internal static class MPersistence
    {
        private static readonly object Sync = new();

        private static Dictionary<string, string> _store = new(StringComparer.Ordinal);

        private static readonly Dictionary<string, IPersistentAttribute> _attributes = new(
            StringComparer.Ordinal
        );

        private static readonly HashSet<string> _dirtyAttributes = new(StringComparer.Ordinal);
        private static readonly HashSet<string> _dirtyOwners = new(StringComparer.Ordinal);

        private struct Meta
        {
            public MPersistencePriority Priority;
            public string SerializerTypeName;
        }

        private static readonly Dictionary<string, Meta> _meta = new(StringComparer.Ordinal);

        // Value envelope (priority + serializer metadata + payload)
        private const string EnvelopePrefix = "pv1|";

        // Wrapper payloads inside the envelope (or standalone if you use SerializeValue directly)
        private const string WrapPrefix = "w1|";
        private const string WrapListPrefix = "wl1|";

        /// <summary>
        /// Clears all registered attributes and the in-memory store.
        /// </summary>
        [StaticClearAction]
        public static void ClearAll()
        {
            _store = new Dictionary<string, string>(StringComparer.Ordinal);
            _attributes.Clear();
            _meta.Clear();
            _dirtyAttributes.Clear();
            _dirtyOwners.Clear();
        }

        public static void AttachStore(Dictionary<string, string> store, bool applyToRegistered)
        {
            lock (Sync)
            {
                _store = store ?? new Dictionary<string, string>(StringComparer.Ordinal);

                Log.Info($"Persist.AttachStore: {_store.Count} entries.");

                if (applyToRegistered)
                    ApplyLoaded_NoLock();
            }
        }

        public static Dictionary<string, string> GetStore()
        {
            lock (Sync)
                return _store;
        }

        public static void Register(IPersistentAttribute attr)
        {
            Register(attr, MPersistencePriority.Normal, serializerTypeName: null);
        }

        /// <summary>
        /// Register an attribute with optional priority + serializer type metadata.
        /// You should call this from MAttribute when persistent==true.
        /// </summary>
        public static void Register(
            IPersistentAttribute attr,
            MPersistencePriority priority,
            string serializerTypeName
        )
        {
            if (attr == null)
                return;

            lock (Sync)
            {
                _attributes[attr.AttributeKey] = attr;

                _meta[attr.AttributeKey] = new Meta
                {
                    Priority = priority,
                    SerializerTypeName = serializerTypeName,
                };

                // If we already loaded a value for this attribute, apply it immediately.
                if (_store.TryGetValue(attr.AttributeKey, out var raw))
                {
                    Unpack(raw, out _, out _, out var payload);

                    TryApply_NoThrow(attr, payload);

                    // Loaded values should not re-mark dirty
                    attr.ClearDirty();
                }
            }
        }

        public static void MarkDirty(IPersistentAttribute attr)
        {
            if (attr == null)
                return;

            lock (Sync)
            {
                _dirtyAttributes.Add(attr.AttributeKey);

                if (!string.IsNullOrEmpty(attr.OwnerKey))
                    _dirtyOwners.Add(attr.OwnerKey);
            }
        }

        public static bool IsOwnerDirty(string ownerKey)
        {
            if (string.IsNullOrEmpty(ownerKey))
                return false;

            lock (Sync)
                return _dirtyOwners.Contains(ownerKey);
        }

        /// <summary>
        /// Writes dirty attribute current values into the store.
        /// Does not remove keys; once a key exists it remains saved unless you explicitly delete it.
        /// </summary>
        public static void FlushDirty()
        {
            lock (Sync)
            {
                if (_dirtyAttributes.Count == 0)
                    return;

                foreach (var key in _dirtyAttributes)
                {
                    if (!_attributes.TryGetValue(key, out var attr))
                        continue;

                    try
                    {
                        var payload = attr.Serialize();

                        _meta.TryGetValue(key, out var meta);

                        var raw = Pack(meta.Priority, meta.SerializerTypeName, payload);

                        _store[key] = raw;

                        Log.Debug(
                            $"Persist.Save: {key} (prio={(int)meta.Priority}, ser={(string.IsNullOrEmpty(meta.SerializerTypeName) ? "-" : meta.SerializerTypeName)})"
                        );

                        attr.ClearDirty();
                    }
                    catch (Exception e)
                    {
                        Log.Exception(e, $"Persist.Save failed for '{key}'.");
                    }
                }

                _dirtyAttributes.Clear();
                _dirtyOwners.Clear();
            }
        }

        public static void ApplyLoaded()
        {
            lock (Sync)
                ApplyLoaded_NoLock();
        }

        private static void ApplyLoaded_NoLock()
        {
            foreach (var pair in _store)
            {
                if (!_attributes.TryGetValue(pair.Key, out var attr))
                    continue;

                Unpack(pair.Value, out var prio, out var serType, out var payload);

                TryApply_NoThrow(attr, payload);
                attr.ClearDirty();

                Log.Debug(
                    $"Persist.Apply(Lazy): {pair.Key} (prio={(int)prio}, ser={(string.IsNullOrEmpty(serType) ? "-" : serType)})"
                );
            }
        }

        /// <summary>
        /// Eagerly applies all persisted values by discovering targets from keys and writing
        /// directly to the underlying MBObject (or to MStore fallback if the member doesn't exist).
        ///
        /// Call this after load (e.g. GameLoadFinished) to mutate CharacterObjects immediately.
        /// </summary>
        public static void ApplyLoadedEager(bool includeStoredFallback = true)
        {
            Dictionary<string, string> snapshot;
            Dictionary<string, Meta> metaSnapshot;

            lock (Sync)
            {
                snapshot = new Dictionary<string, string>(_store, StringComparer.Ordinal);
                metaSnapshot = new Dictionary<string, Meta>(_meta, StringComparer.Ordinal);
            }

            if (snapshot.Count == 0)
            {
                Log.Info("Persist.ApplyEager: store is empty.");
                return;
            }

            var entries = new List<PersistedEntry>(snapshot.Count);

            foreach (var kvp in snapshot)
            {
                if (
                    !TryParseKey(
                        kvp.Key,
                        out var wrapperTypeName,
                        out var stringId,
                        out var attrName,
                        out var valueTypeName
                    )
                )
                    continue;

                Unpack(kvp.Value, out var prio, out var serTypeName, out var payload);

                // If no envelope exists, fall back to registered meta (if any)
                if (
                    !kvp.Value.StartsWith(EnvelopePrefix, StringComparison.Ordinal)
                    && metaSnapshot.TryGetValue(kvp.Key, out var meta)
                )
                {
                    prio = meta.Priority;
                    serTypeName = meta.SerializerTypeName;
                }

                entries.Add(
                    new PersistedEntry
                    {
                        Key = kvp.Key,
                        WrapperTypeName = wrapperTypeName,
                        StringId = stringId,
                        AttributeName = attrName,
                        ValueTypeName = valueTypeName,
                        Priority = prio,
                        SerializerTypeName = serTypeName,
                        Payload = payload,
                    }
                );
            }

            entries.Sort(
                (a, b) =>
                {
                    var c = ((int)a.Priority).CompareTo((int)b.Priority);
                    if (c != 0)
                        return c;

                    c = string.CompareOrdinal(a.WrapperTypeName, b.WrapperTypeName);
                    if (c != 0)
                        return c;

                    c = string.CompareOrdinal(a.StringId, b.StringId);
                    if (c != 0)
                        return c;

                    return string.CompareOrdinal(a.AttributeName, b.AttributeName);
                }
            );

            Log.Info($"Persist.ApplyEager: applying {entries.Count} entries...");

            foreach (var e in entries)
            {
                try
                {
                    ApplyEntryEager(e, includeStoredFallback);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"Persist.ApplyEager failed: {e.Key}");
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Eager apply helpers                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private struct PersistedEntry
        {
            public string Key;
            public string WrapperTypeName;
            public string StringId;
            public string AttributeName;
            public string ValueTypeName;
            public MPersistencePriority Priority;
            public string SerializerTypeName;
            public string Payload;
        }

        private static void ApplyEntryEager(PersistedEntry e, bool includeStoredFallback)
        {
            var wrapperType = ResolveType(e.WrapperTypeName);
            if (wrapperType == null)
            {
                Log.Warn(
                    $"Persist.ApplyEager: wrapper type not found: {e.WrapperTypeName} ({e.Key})"
                );
                return;
            }

            var baseType = GetWrappedBaseType(wrapperType);
            if (baseType == null)
            {
                Log.Warn(
                    $"Persist.ApplyEager: cannot resolve wrapped base type for {wrapperType.FullName} ({e.Key})"
                );
                return;
            }

            var baseObj = GetMBObject(baseType, e.StringId);
            if (baseObj == null)
            {
                Log.Warn(
                    $"Persist.ApplyEager: MBObject not found: {baseType.FullName}:{e.StringId} ({e.Key})"
                );
                return;
            }

            var valueType = ResolveType(e.ValueTypeName) ?? typeof(string);

            var value = DeserializeWithOptionalSerializer(
                e.Payload,
                e.SerializerTypeName,
                valueType
            );

            // Apply to underlying property/field if it exists
            if (Reflection.HasProperty(baseObj, e.AttributeName))
            {
                var prop = baseObj
                    .GetType()
                    .GetProperty(
                        e.AttributeName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                var normalized = NormalizeForMemberAssignment(value, prop?.PropertyType);
                Reflection.SetPropertyValue(baseObj, e.AttributeName, normalized);

                Log.Debug($"Persist.ApplyEager: {e.Key} (prio={(int)e.Priority}) -> property");
                return;
            }

            if (Reflection.HasField(baseObj, e.AttributeName))
            {
                var field = baseObj
                    .GetType()
                    .GetField(
                        e.AttributeName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                var normalized = NormalizeForMemberAssignment(value, field?.FieldType);
                Reflection.SetFieldValue(baseObj, e.AttributeName, normalized);

                Log.Debug($"Persist.ApplyEager: {e.Key} (prio={(int)e.Priority}) -> field");
                return;
            }

            // Fallback: stored attribute (same key format as BuildStoredKey<T>)
            if (includeStoredFallback)
            {
                try
                {
                    MStoreSet(valueType, e.Key, value);
                    Log.Debug($"Persist.ApplyEager: {e.Key} (prio={(int)e.Priority}) -> MStore");
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"Persist.ApplyEager: failed to set MStore for '{e.Key}'.");
                }
            }
        }

        private static object NormalizeForMemberAssignment(object value, Type destinationType)
        {
            if (value == null || destinationType == null)
                return value;

            // If persisted value is a wrapper but destination expects MBObjectBase, write wrapper.Base
            var vt = value.GetType();
            if (IsWrapperType(vt) && typeof(MBObjectBase).IsAssignableFrom(destinationType))
            {
                if (Reflection.HasProperty(value, "Base"))
                    return Reflection.GetPropertyValue<object>(value, "Base");
            }

            return value;
        }

        private static object DeserializeWithOptionalSerializer(
            string payload,
            string serializerTypeName,
            Type valueType
        )
        {
            // Allow serializerTypeName to reference any object with:
            //   object Deserialize(string)
            if (!string.IsNullOrEmpty(serializerTypeName))
            {
                var st = ResolveType(serializerTypeName);
                if (st != null)
                {
                    try
                    {
                        var ser = Activator.CreateInstance(st);
                        var mi = st.GetMethod(
                            "Deserialize",
                            BindingFlags.Instance | BindingFlags.Public,
                            null,
                            new[] { typeof(string) },
                            null
                        );

                        if (mi != null)
                            return mi.Invoke(ser, new object[] { payload });
                    }
                    catch (Exception e)
                    {
                        Log.Exception(
                            e,
                            $"Persist.Deserialize: serializer failed ({serializerTypeName}). Falling back to default."
                        );
                    }
                }
            }

            return DeserializeValue(payload, valueType);
        }

        private static bool TryParseKey(
            string key,
            out string wrapperTypeName,
            out string stringId,
            out string attrName,
            out string valueTypeName
        )
        {
            wrapperTypeName = null;
            stringId = null;
            attrName = null;
            valueTypeName = null;

            // WrapperType:StringId:AttrName:ValueType
            var parts = key.Split(new[] { ':' }, 4);
            if (parts.Length != 4)
                return false;

            wrapperTypeName = parts[0];
            stringId = parts[1];
            attrName = parts[2];
            valueTypeName = parts[3];
            return true;
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

        private static Type GetWrappedBaseType(Type wrapperType)
        {
            var cur = wrapperType;
            while (cur != null && cur != typeof(object))
            {
                if (cur.IsGenericType && cur.GetGenericTypeDefinition() == typeof(WBase<,>))
                {
                    var args = cur.GetGenericArguments();
                    if (args.Length == 2)
                        return args[1]; // TBase (MBObjectBase)
                }

                cur = cur.BaseType;
            }

            return null;
        }

        private static object GetMBObject(Type mbType, string stringId)
        {
            var mgr = MBObjectManager.Instance;
            if (mgr == null || mbType == null || stringId == null)
                return null;

            // Pick: T GetObject<T>(string objectName)
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

            var g = mi.MakeGenericMethod(mbType);
            return g.Invoke(mgr, new object[] { stringId });
        }

        private static void MStoreSet(Type valueType, string key, object value)
        {
            var mi = typeof(MStore)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "Set"
                    && m.IsGenericMethodDefinition
                    && m.GetGenericArguments().Length == 1
                    && m.GetParameters().Length == 2
                    && m.GetParameters()[0].ParameterType == typeof(string)
                );

            if (mi == null)
                throw new MissingMethodException("MStore.Set<T>(string key, T value) not found.");

            var g = mi.MakeGenericMethod(valueType);
            g.Invoke(null, new object[] { key, value });
        }

        private static void TryApply_NoThrow(IPersistentAttribute attr, string serialized)
        {
            try
            {
                attr.ApplySerialized(serialized);
            }
            catch (Exception e)
            {
                Log.Exception(
                    e,
                    $"Failed to apply persistent attribute '{attr.AttributeKey}' (owner '{attr.OwnerKey}')."
                );
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                  Envelope helpers                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static string Escape(string s) => s == null ? "" : Uri.EscapeDataString(s);

        private static string Unescape(string s) =>
            string.IsNullOrEmpty(s) ? "" : Uri.UnescapeDataString(s);

        private static string Pack(
            MPersistencePriority prio,
            string serializerTypeName,
            string payload
        )
        {
            // pv1|<prioInt>|<serializerType>|<payload>
            return $"{EnvelopePrefix}{(int)prio}|{Escape(serializerTypeName ?? "")}|{Escape(payload ?? "")}";
        }

        private static void Unpack(
            string raw,
            out MPersistencePriority prio,
            out string serializerTypeName,
            out string payload
        )
        {
            prio = MPersistencePriority.Normal;
            serializerTypeName = null;
            payload = raw;

            if (
                string.IsNullOrEmpty(raw)
                || !raw.StartsWith(EnvelopePrefix, StringComparison.Ordinal)
            )
                return;

            var rest = raw.Substring(EnvelopePrefix.Length);
            var split = rest.Split(new[] { '|' }, 3);

            if (split.Length >= 1 && int.TryParse(split[0], out var p))
                prio = (MPersistencePriority)p;

            serializerTypeName = split.Length >= 2 ? Unescape(split[1]) : null;
            payload = split.Length >= 3 ? Unescape(split[2]) : "";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Default string conversion              //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

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

            return Reflection.GetPropertyValue<string>(wrapper, "StringId");
        }

        private static object InvokeWrapperGet(Type wrapperType, string stringId)
        {
            if (wrapperType == null || stringId == null)
                return null;

            var mi = wrapperType.GetMethod(
                "Get",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null
            );

            if (mi == null)
                return null;

            return mi.Invoke(null, new object[] { stringId });
        }

        public static string SerializeValue(object value, Type type)
        {
            if (value == null)
                return null;

            // If someone accidentally passes an already-packed envelope, keep it
            if (
                type == typeof(string)
                && value is string s
                && s.StartsWith(EnvelopePrefix, StringComparison.Ordinal)
            )
                return s;

            // WBase<TWrapper, TBase>
            if (IsWrapperType(type))
            {
                var id = GetWrapperStringId(value);
                var tName = value.GetType().FullName;
                return $"{WrapPrefix}{tName}|{Escape(id)}";
            }

            // IEnumerable<WBase>
            if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
            {
                var elemType = GetEnumerableElementType(type);

                // If declared type is non-generic IEnumerable, infer from runtime items
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
                $"No default serializer for type '{type.FullName}'. Provide a serializer or store a supported type."
            );
        }

        public static object DeserializeValue(string serialized, Type type)
        {
            if (serialized == null)
                return null;

            // Allow callers to pass raw envelopes by mistake
            if (serialized.StartsWith(EnvelopePrefix, StringComparison.Ordinal))
            {
                Unpack(serialized, out _, out _, out var payload);
                serialized = payload;
            }

            // Wrapper single
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

            // Wrapper list
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

                if (type.IsAssignableFrom(listType))
                    return list;

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
                // IMPORTANT: avoid AmbiguousMatchException by selecting the exact overload
                var mgr = MBObjectManager.Instance;
                if (mgr == null)
                    return null;

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
                $"No default deserializer for type '{type.FullName}'. Provide a serializer or store a supported type."
            );
        }
    }
}
