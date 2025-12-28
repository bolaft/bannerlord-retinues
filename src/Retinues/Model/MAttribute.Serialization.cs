using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Retinues.Utilities;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model
{
    public partial class MAttribute<T>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Reflection cache                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static readonly object CacheLock = new();

        static MethodInfo _mbGetObjectGeneric;

        static readonly Dictionary<Type, bool> IsWrapperCache = new();
        static readonly Dictionary<Type, MethodInfo> WrapperGetCache = new();

        static MethodInfo GetMbGetObjectGeneric()
        {
            if (_mbGetObjectGeneric != null)
                return _mbGetObjectGeneric;

            lock (CacheLock)
            {
                if (_mbGetObjectGeneric != null)
                    return _mbGetObjectGeneric;

                _mbGetObjectGeneric = typeof(MBObjectManager)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                        m.Name == "GetObject"
                        && m.IsGenericMethodDefinition
                        && m.GetGenericArguments().Length == 1
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(string)
                    );

                return _mbGetObjectGeneric;
            }
        }

        static bool IsWBaseType(Type type)
        {
            if (type == null)
                return false;

            lock (CacheLock)
            {
                if (IsWrapperCache.TryGetValue(type, out var cached))
                    return cached;
            }

            var t = type;
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(WBase<,>))
                {
                    lock (CacheLock)
                        IsWrapperCache[type] = true;

                    return true;
                }

                t = t.BaseType;
            }

            lock (CacheLock)
                IsWrapperCache[type] = false;

            return false;
        }

        static MethodInfo GetWrapperGetMethod(Type wrapperType)
        {
            if (wrapperType == null)
                return null;

            lock (CacheLock)
            {
                if (WrapperGetCache.TryGetValue(wrapperType, out var mi))
                    return mi;
            }

            var found = wrapperType.GetMethod(
                "Get",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                binder: null,
                types: [typeof(string)],
                modifiers: null
            );

            lock (CacheLock)
                WrapperGetCache[wrapperType] = found;

            return found;
        }

        static MBObjectBase ResolveMbObject(Type objectType, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "null")
                return null;

            var mgr = MBObjectManager.Instance;
            if (mgr == null)
                return null;

            var getObjectGeneric = GetMbGetObjectGeneric();
            if (getObjectGeneric == null)
                return null;

            try
            {
                return getObjectGeneric.MakeGenericMethod(objectType).Invoke(mgr, [id])
                    as MBObjectBase;
            }
            catch
            {
                return null;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 String serialization (compact)          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public string Serialize()
        {
            try
            {
                var value = Get();

                if (value == null)
                    return null;

                // Single MBObjectBase: store StringId
                if (value is MBObjectBase mbo)
                    return mbo.StringId ?? string.Empty;

                // Single IModel (MBase / WBase / etc.)
                if (value is IModel model)
                    return SerializeModel(model);

                // Collections (List<>, etc; but not string)
                if (value is IEnumerable enumerable && value is not string)
                    return SerializeEnumerable(enumerable);

                // Plain CLR type
                return Serialization.Serialize(value).Compact;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to serialize MAttribute.");
                return string.Empty;
            }
        }

        static string SerializeModel(object model)
        {
            if (model == null)
                return null;

            var type = model.GetType();

            // WBase<,> wrappers: always store underlying MBObjectBase.StringId
            if (IsWBaseType(type))
            {
                var baseProp = type.GetProperty(
                    "Base",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (baseProp?.GetValue(model) is MBObjectBase baseMbo)
                {
                    var id = baseMbo.StringId;
                    return string.IsNullOrEmpty(id) ? null : id;
                }

                return null;
            }

            // Non-WBase IModel -> prefer a custom Serialize()
            var serializeMi = type.GetMethod(
                "Serialize",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null
            );

            if (serializeMi != null)
            {
                var r = serializeMi.Invoke(model, null) as string;
                return string.IsNullOrEmpty(r) ? null : r;
            }

            // Fallback: generic XML serialization
            return Serialization.Serialize(model).Compact;
        }

        static string SerializeEnumerable(IEnumerable enumerable)
        {
            // Serialize collections as List<string> then XML-encode that list.
            var items = new List<string>();

            foreach (var item in enumerable)
            {
                if (item == null)
                {
                    items.Add(string.Empty);
                    continue;
                }

                // MBObjectBase in a list -> StringId
                if (item is MBObjectBase itemMbo)
                {
                    items.Add(itemMbo.StringId ?? string.Empty);
                    continue;
                }

                var itemType = item.GetType();

                // WBase<,> wrapper in a list -> Base.StringId
                if (item is IModel && IsWBaseType(itemType))
                {
                    var baseProp = itemType.GetProperty(
                        "Base",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                    if (baseProp?.GetValue(item) is MBObjectBase baseMbo)
                    {
                        items.Add(baseMbo.StringId ?? string.Empty);
                        continue;
                    }
                }

                // Other IModel instances -> let it serialize itself if it has Serialize()
                if (item is IModel)
                {
                    var mi = itemType.GetMethod(
                        "Serialize",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null,
                        types: Type.EmptyTypes,
                        modifiers: null
                    );

                    if (mi != null)
                    {
                        var inner = mi.Invoke(item, null) as string;
                        items.Add(inner ?? string.Empty);
                        continue;
                    }
                }

                // Fallback: generic XML for arbitrary CLR item
                items.Add(Serialization.Serialize(item).Compact ?? string.Empty);
            }

            return Serialization.Serialize(items).Compact;
        }

        public string Deserialize(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                {
                    SetValue(default, markDirty: true);
                    return data;
                }

                var t = typeof(T);

                // Single MBObjectBase
                if (typeof(MBObjectBase).IsAssignableFrom(t))
                {
                    var obj = ResolveMbObject(t, data);
                    SetValue(obj == null ? default : (T)(object)obj, markDirty: true);
                    return data;
                }

                // Single WBase<,> wrapper: stored as Base.StringId, resolve via static Get(string)
                if (IsWBaseType(t))
                {
                    DeserializeSingleWrapper(t, data);
                    return data;
                }

                // List<MBObjectBase> or List<WBase<,>>
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elemType = t.GetGenericArguments()[0];

                    if (typeof(MBObjectBase).IsAssignableFrom(elemType))
                    {
                        DeserializeMbObjectList(elemType, data);
                        return data;
                    }

                    if (IsWBaseType(elemType))
                    {
                        DeserializeWrapperList(elemType, data);
                        return data;
                    }
                }

                // Non-WBase IModel: call Deserialize on the existing instance if present
                if (typeof(IModel).IsAssignableFrom(t) && !IsWBaseType(t))
                {
                    var current = Get();
                    if (current != null)
                    {
                        var mi = current
                            .GetType()
                            .GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Instance);

                        if (mi != null)
                        {
                            mi.Invoke(current, [data]);
                            return data;
                        }
                    }
                }

                // Fallback: generic deserialization
                var deserialized = Serialization.Deserialize<T>(data);
                SetValue(deserialized, markDirty: true);
                return data;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to deserialize MAttribute.");
                return data;
            }
        }

        void DeserializeSingleWrapper(Type wrapperType, string data)
        {
            try
            {
                var id = data?.Trim();
                if (string.IsNullOrEmpty(id))
                {
                    SetValue(default, true);
                    return;
                }

                var get = GetWrapperGetMethod(wrapperType);

                object wrapper = null;
                if (get != null)
                    wrapper = get.Invoke(null, [id]);

                if (wrapper is T typed)
                    SetValue(typed, true);
                else
                    SetValue(default, true);
            }
            catch (Exception e)
            {
                Log.Exception(
                    e,
                    $"MAttribute.Deserialize: failed to deserialize WBase attribute '{Name}' from '{data}'."
                );
                SetValue(default, true);
            }
        }

        void DeserializeMbObjectList(Type elemType, string data)
        {
            var ids = Serialization.Deserialize<List<string>>(data) ?? new List<string>();
            var listType = typeof(List<>).MakeGenericType(elemType);
            var list = (IList)Activator.CreateInstance(listType);

            foreach (var id in ids)
            {
                var obj = ResolveMbObject(elemType, id);
                list.Add(obj);
            }

            SetValue((T)list, markDirty: true);
        }

        void DeserializeWrapperList(Type elemType, string data)
        {
            try
            {
                var ids = Serialization.Deserialize<List<string>>(data) ?? new List<string>();

                var listType = typeof(List<>).MakeGenericType(elemType);
                var resultList = (IList)Activator.CreateInstance(listType);

                var get = GetWrapperGetMethod(elemType);

                if (get != null)
                {
                    for (int i = 0; i < ids.Count; i++)
                    {
                        var rawId = ids[i];
                        if (string.IsNullOrWhiteSpace(rawId))
                            continue;

                        var wrapper = get.Invoke(null, [rawId.Trim()]);
                        if (wrapper != null)
                            resultList.Add(wrapper);
                    }
                }

                SetValue((T)resultList, true);
            }
            catch (Exception e)
            {
                Log.Exception(
                    e,
                    $"MAttribute.Deserialize: failed to deserialize List<WBase> attribute '{Name}'."
                );
                SetValue(default, true);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                XML element serialization (MBase)        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public XElement SerializeXml()
        {
            var elementName = Name;

            var value = Get();
            if (value == null)
                return null;

            var el = new XElement(elementName);

            if (value is string s)
            {
                el.SetAttributeValue("t", "string");
                el.Value = s;
                return el;
            }

            if (value is TextObject to)
            {
                el.SetAttributeValue("t", "text");
                el.Value = to?.Value ?? string.Empty;
                return el;
            }

            if (value is Enum)
            {
                el.SetAttributeValue("t", "enum");
                el.SetValue(value.ToString()); // "Infantry", "Cavalry", ...
                return el;
            }

            if (value is bool b)
            {
                el.SetAttributeValue("t", "bool");
                el.Value = b ? "true" : "false";
                return el;
            }

            if (value is int i)
            {
                el.SetAttributeValue("t", "int");
                el.Value = i.ToString(Inv);
                return el;
            }

            if (value is float f)
            {
                el.SetAttributeValue("t", "float");
                el.Value = f.ToString("R", Inv);
                return el;
            }

            if (value is double d)
            {
                el.SetAttributeValue("t", "double");
                el.Value = d.ToString("R", Inv);
                return el;
            }

            // MBObjectBase reference -> store StringId
            if (value is MBObjectBase mbo)
            {
                el.SetAttributeValue("t", "mb");
                el.Value = mbo.StringId ?? string.Empty;
                return el;
            }

            // Wrapper or other IModel
            if (value is IModel model)
            {
                el.SetAttributeValue("t", "model");

                var inner = SerializeModel(model);
                if (!string.IsNullOrWhiteSpace(inner) && inner.TrimStart().StartsWith("<"))
                    el.Add(XElement.Parse(inner));
                else
                    el.Value = inner ?? string.Empty;

                return el;
            }

            // IEnumerable -> <Attr><Item>..</Item></Attr>
            if (value is IEnumerable enumerable && value is not string)
            {
                el.SetAttributeValue("t", "list");

                foreach (var item in enumerable)
                {
                    if (item == null)
                    {
                        el.Add(new XElement("Item"));
                        continue;
                    }

                    if (item is MBObjectBase itemMbo)
                    {
                        el.Add(new XElement("Item", itemMbo.StringId ?? string.Empty));
                        continue;
                    }

                    var itemType = item.GetType();

                    if (item is IModel && IsWBaseType(itemType))
                    {
                        var baseProp = itemType.GetProperty(
                            "Base",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );

                        if (baseProp?.GetValue(item) is MBObjectBase baseMbo)
                        {
                            el.Add(new XElement("Item", baseMbo.StringId ?? string.Empty));
                            continue;
                        }

                        var idProp = itemType.GetProperty(
                            "StringId",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        );

                        var id = idProp?.GetValue(item) as string;
                        el.Add(new XElement("Item", id ?? string.Empty));
                        continue;
                    }

                    if (item is TextObject itemTo)
                    {
                        el.Add(new XElement("Item", itemTo?.Value ?? string.Empty));
                        continue;
                    }

                    if (item is IModel itemModel)
                    {
                        var inner = SerializeModel(itemModel);
                        el.Add(new XElement("Item", inner ?? string.Empty));
                        continue;
                    }

                    if (item is string str)
                    {
                        var ts = str.TrimStart();

                        if (ts.StartsWith("<"))
                        {
                            try
                            {
                                el.Add(XElement.Parse(str));
                                continue;
                            }
                            catch { }
                        }

                        el.Add(new XElement("Item", str));
                        continue;
                    }

                    el.Add(new XElement("Item", Convert.ToString(item, Inv) ?? string.Empty));
                }

                return el;
            }

            el.SetAttributeValue("t", "string");
            el.Value = Convert.ToString(value, Inv) ?? string.Empty;
            return el;
        }

        public void DeserializeXml(XElement el)
        {
            if (el == null)
                return;

            // Type tag used by your exports: list/bool/int/float/string/text/mb
            var tag = ((string)el.Attribute("t") ?? string.Empty).Trim();

            // Some of your serializers might store scalar value in an attribute; prefer it if present.
            var raw = (string)el.Attribute("v") ?? el.Value;
            raw = (raw ?? string.Empty).Trim();

            // Local helpers to keep this method self-contained.

            void MarkCleanIfPossible()
            {
                try
                {
                    // Try common dirty flags/properties. No-op if they don't exist.
                    if (Reflection.HasField(this, "_isDirty"))
                        Reflection.SetFieldValue(this, "_isDirty", false);
                    if (Reflection.HasField(this, "_dirty"))
                        Reflection.SetFieldValue(this, "_dirty", false);

                    if (Reflection.HasProperty(this, "IsDirty"))
                        Reflection.SetPropertyValue(this, "IsDirty", false);

                    // Some builds might have MarkClean/MarkDirty APIs.
                    try
                    {
                        Reflection.InvokeMethod(this, "MarkClean", null);
                    }
                    catch { }
                }
                catch
                {
                    // best effort; never throw during deserialization
                }
            }

            bool TryAssignQuiet(object valueObj)
            {
                try
                {
                    // Prefer writing backing field/property directly to avoid triggering "Set" side effects.
                    if (Reflection.HasField(this, "_value"))
                    {
                        Reflection.SetFieldValue(this, "_value", valueObj);
                        MarkCleanIfPossible();
                        return true;
                    }

                    if (Reflection.HasProperty(this, "Value"))
                    {
                        Reflection.SetPropertyValue(this, "Value", valueObj);
                        MarkCleanIfPossible();
                        return true;
                    }
                }
                catch
                {
                    // ignore and fallback to Set(...)
                }

                return false;
            }

            void Assign(T value)
            {
                if (!TryAssignQuiet(value))
                {
                    // Fallback: use normal Set and then attempt to mark clean.
                    Set(value);
                    MarkCleanIfPossible();
                }
            }

            object ResolveMbObject(Type mbType, string id)
            {
                if (string.IsNullOrWhiteSpace(id) || mbType == null)
                    return null;

                try
                {
                    // MBObjectManager.Instance.GetObject<T>(string)
                    var mgr = MBObjectManager.Instance;
                    var mi = typeof(MBObjectManager)
                        .GetMethods()
                        .FirstOrDefault(m =>
                            m.Name == "GetObject"
                            && m.IsGenericMethodDefinition
                            && m.GetParameters().Length == 1
                            && m.GetParameters()[0].ParameterType == typeof(string)
                        );

                    if (mi == null)
                        return null;

                    var g = mi.MakeGenericMethod(mbType);
                    return g.Invoke(mgr, new object[] { id });
                }
                catch
                {
                    return null;
                }
            }

            object ParseScalar(string s, Type targetType)
            {
                s = (s ?? string.Empty).Trim();

                if (targetType == typeof(string))
                    return s;

                if (targetType == typeof(TextObject))
                    return new TextObject(s);

                if (targetType == typeof(bool))
                    return bool.Parse(s);

                if (targetType == typeof(int))
                    return int.Parse(s, CultureInfo.InvariantCulture);

                if (targetType == typeof(float))
                    return float.Parse(s, CultureInfo.InvariantCulture);

                if (targetType.IsEnum)
                    return Enum.Parse(targetType, s, ignoreCase: true);

                // MBObjectBase-derived (CultureObject, ItemCategory, etc.)
                if (typeof(MBObjectBase).IsAssignableFrom(targetType))
                    return ResolveMbObject(targetType, s);

                // Last resort: try Convert
                try
                {
                    return Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return null;
                }
            }

            object ParseListFromChildren(XElement listEl, Type listType)
            {
                // listType: List<TItem>, MBBindingList<TItem>, TItem[], etc.
                if (listType == null)
                    return null;

                var isArray = listType.IsArray;
                var isGeneric = listType.IsGenericType;
                var itemType =
                    (
                        isArray ? listType.GetElementType()
                        : isGeneric ? listType.GetGenericArguments().FirstOrDefault()
                        : typeof(string)
                    ) ?? typeof(string);

                // We'll parse into a temporary List<TItem> first.
                var tmpListType = typeof(List<>).MakeGenericType(itemType);
                var tmp = (IList)Activator.CreateInstance(tmpListType);

                foreach (var child in listEl.Elements())
                {
                    // <Item>primitive</Item> or <MEquipment ...>...</MEquipment>
                    object itemObj = null;

                    if (!child.HasElements && child.Name.LocalName == "Item")
                    {
                        itemObj = ParseScalar(child.Value, itemType);
                    }
                    else if (
                        !child.HasElements
                        && child.Attributes().Count() == 0
                        && itemType == typeof(string)
                    )
                    {
                        itemObj = (child.Value ?? string.Empty).Trim();
                    }
                    else
                    {
                        // Complex model: create instance and ApplyXml(child)
                        try
                        {
                            var inst = Activator.CreateInstance(itemType);
                            if (inst != null)
                            {
                                // IMPORTANT: your Reflection.InvokeMethod signature needs genericTypes as arg #3:
                                // InvokeMethod(object target, string name, Type[] genericTypes, params object[] args)
                                Reflection.InvokeMethod(inst, "ApplyXml", null, child);
                                itemObj = inst;
                            }
                        }
                        catch
                        {
                            itemObj = null;
                        }
                    }

                    if (itemObj != null)
                        tmp.Add(itemObj);
                }

                // Convert tmp list into the declared listType.
                if (isArray)
                {
                    var arr = Array.CreateInstance(itemType, tmp.Count);
                    tmp.CopyTo(arr, 0);
                    return arr;
                }

                // Handle MBBindingList<T>
                if (
                    isGeneric
                    && listType.GetGenericTypeDefinition().FullName
                        == "TaleWorlds.Library.MBBindingList`1"
                )
                {
                    var bb = Activator.CreateInstance(listType);
                    var add = listType.GetMethod("Add");
                    if (bb != null && add != null)
                    {
                        for (int i = 0; i < tmp.Count; i++)
                            add.Invoke(bb, new[] { tmp[i] });
                        return bb;
                    }
                }

                // If declared type is List<T> (or any assignable from List<T>), return tmp.
                if (listType.IsAssignableFrom(tmpListType))
                    return tmp;

                // Try to construct the declared list type and add items.
                try
                {
                    if (typeof(IList).IsAssignableFrom(listType))
                    {
                        var list = (IList)Activator.CreateInstance(listType);
                        if (list != null)
                        {
                            for (int i = 0; i < tmp.Count; i++)
                                list.Add(tmp[i]);
                            return list;
                        }
                    }
                }
                catch
                {
                    // ignore
                }

                return tmp;
            }

            try
            {
                // 1) Lists with nested children: never DataContract-deserialize.
                if (tag == "list" && el.HasElements)
                {
                    var obj = ParseListFromChildren(el, typeof(T));
                    if (obj is T typed)
                    {
                        Assign(typed);
                        return;
                    }

                    // If we couldn't strongly-type it, try best-effort assign via backing field/property.
                    if (TryAssignQuiet(obj))
                        return;

                    // Last resort: ignore.
                    return;
                }

                // 2) Scalar types: parse directly (avoids DCS on non-XML strings).
                if (tag == "bool")
                {
                    Assign((T)(object)bool.Parse(raw));
                    return;
                }

                if (tag == "int")
                {
                    Assign((T)(object)int.Parse(raw, CultureInfo.InvariantCulture));
                    return;
                }

                if (tag == "float")
                {
                    Assign((T)(object)float.Parse(raw, CultureInfo.InvariantCulture));
                    return;
                }

                if (tag == "string")
                {
                    Assign((T)(object)raw);
                    return;
                }

                if (tag == "text")
                {
                    Assign((T)(object)new TextObject(raw));
                    return;
                }

                if (tag == "mb")
                {
                    // Most of your "mb" values are StringId references, e.g. "nord".
                    var mb = ResolveMbObject(typeof(T), raw);
                    if (mb is T typedMb)
                    {
                        Assign(typedMb);
                        return;
                    }

                    // Some mb-like attrs might actually be stored as string IDs in a string attribute type.
                    if (typeof(T) == typeof(string))
                    {
                        Assign((T)(object)raw);
                        return;
                    }

                    // If resolution failed, keep it clean but default.
                    MarkCleanIfPossible();
                    return;
                }

                if (tag == "enum")
                {
                    // accept both "Cavalry" and "2"
                    if (
                        int.TryParse(
                            raw,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var i
                        )
                    )
                    {
                        Assign((T)Enum.ToObject(typeof(T), i));
                        return;
                    }

                    Assign((T)Enum.Parse(typeof(T), raw, ignoreCase: true));
                    return;
                }

                // 3) Fallback: if the old format stored values as inner text with unknown tag,
                // try best-effort scalar conversion based on T.
                var fallbackObj = ParseScalar(raw, typeof(T));
                if (fallbackObj is T fallbackTyped)
                {
                    Assign(fallbackTyped);
                    return;
                }

                // If nothing worked, keep it clean and do nothing.
                MarkCleanIfPossible();
            }
            catch (Exception ex)
            {
                // Never throw from deserialization.
                try
                {
                    Log.Warn($"MAttribute.DeserializeXml failed for '{el.Name}': {ex.Message}");
                }
                catch
                {
                    // ignore logging failures
                }
            }
        }
    }
}
