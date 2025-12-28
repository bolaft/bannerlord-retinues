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

            var tag = ((string)el.Attribute("t") ?? string.Empty).Trim();

            // Prefer v= if present, otherwise inner text.
            var raw = (string)el.Attribute("v") ?? el.Value;
            raw = (raw ?? string.Empty).Trim();

            void MarkCleanIfPossible()
            {
                try
                {
                    if (Reflection.HasField(this, "_isDirty"))
                        Reflection.SetFieldValue(this, "_isDirty", false);
                    if (Reflection.HasField(this, "_dirty"))
                        Reflection.SetFieldValue(this, "_dirty", false);

                    if (Reflection.HasProperty(this, "IsDirty"))
                        Reflection.SetPropertyValue(this, "IsDirty", false);

                    try
                    {
                        Reflection.InvokeMethod(this, "MarkClean", null);
                    }
                    catch { }
                }
                catch
                {
                    // best effort
                }
            }

            bool TryAssignQuiet(object valueObj)
            {
                try
                {
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
                    // ignore
                }

                return false;
            }

            // IMPORTANT: prefer Set(.) so side-effects apply to Base objects.
            // Quiet assign is only fallback if Set throws.
            void Assign(T value)
            {
                try
                {
                    Set(value);
                    MarkCleanIfPossible();
                }
                catch
                {
                    _ = TryAssignQuiet(value);
                }
            }

            object ResolveMbObject(Type mbType, string id)
            {
                if (string.IsNullOrWhiteSpace(id) || mbType == null)
                    return null;

                try
                {
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

                if (targetType == typeof(double))
                    return double.Parse(s, CultureInfo.InvariantCulture);

                if (targetType.IsEnum)
                    return Enum.Parse(targetType, s, ignoreCase: true);

                if (typeof(MBObjectBase).IsAssignableFrom(targetType))
                    return ResolveMbObject(targetType, s);

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

                var tmpListType = typeof(List<>).MakeGenericType(itemType);
                var tmp = (IList)Activator.CreateInstance(tmpListType);

                var wrapperGet = IsWBaseType(itemType) ? GetWrapperGetMethod(itemType) : null;

                foreach (var child in listEl.Elements())
                {
                    object itemObj = null;

                    // <Item>...</Item>
                    if (child.Name.LocalName == "Item")
                    {
                        // <Item><SomeXml /></Item> (rare, but support it)
                        if (child.HasElements)
                        {
                            var first = child.Elements().FirstOrDefault();

                            if (first != null)
                            {
                                if (itemType == typeof(string))
                                {
                                    itemObj = first.ToString(SaveOptions.DisableFormatting);
                                }
                                else
                                {
                                    try
                                    {
                                        var inst = Activator.CreateInstance(itemType);
                                        if (inst != null)
                                        {
                                            Reflection.InvokeMethod(inst, "ApplyXml", null, first);
                                            itemObj = inst;
                                        }
                                    }
                                    catch
                                    {
                                        itemObj = null;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var v = (child.Value ?? string.Empty).Trim();

                            // List of wrappers: Item contains StringId
                            if (wrapperGet != null)
                            {
                                try
                                {
                                    itemObj = wrapperGet.Invoke(null, new object[] { v });
                                }
                                catch
                                {
                                    itemObj = null;
                                }
                            }
                            // List of MBObjects: Item contains StringId
                            else if (typeof(MBObjectBase).IsAssignableFrom(itemType))
                            {
                                itemObj = ResolveMbObject(itemType, v);
                            }
                            // List of models: Item may contain an XML string
                            else if (typeof(IModel).IsAssignableFrom(itemType) && v.StartsWith("<"))
                            {
                                try
                                {
                                    var inst = Activator.CreateInstance(itemType);
                                    if (inst != null)
                                    {
                                        // Prefer Deserialize(string) if it exists.
                                        try
                                        {
                                            Reflection.InvokeMethod(inst, "Deserialize", null, v);
                                        }
                                        catch
                                        {
                                            var root = XElement.Parse(v);
                                            Reflection.InvokeMethod(inst, "ApplyXml", null, root);
                                        }

                                        itemObj = inst;
                                    }
                                }
                                catch
                                {
                                    itemObj = null;
                                }
                            }
                            else
                            {
                                itemObj = ParseScalar(v, itemType);
                            }
                        }
                    }
                    // <SomeModel ...>...</SomeModel> OR direct XML list items when itemType is string
                    else
                    {
                        // CRITICAL: List<string> can be serialized as direct child elements (e.g. <MEquipment .../>)
                        if (itemType == typeof(string))
                        {
                            itemObj = child.ToString(SaveOptions.DisableFormatting);
                        }
                        else
                        {
                            try
                            {
                                var inst = Activator.CreateInstance(itemType);
                                if (inst != null)
                                {
                                    Reflection.InvokeMethod(inst, "ApplyXml", null, child);
                                    itemObj = inst;
                                }
                            }
                            catch
                            {
                                itemObj = null;
                            }
                        }
                    }

                    if (itemObj != null)
                        tmp.Add(itemObj);
                }

                if (isArray)
                {
                    var arr = Array.CreateInstance(itemType, tmp.Count);
                    tmp.CopyTo(arr, 0);
                    return arr;
                }

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

                if (listType.IsAssignableFrom(tmpListType))
                    return tmp;

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
                // 0) MODEL (this was missing and is why Equipments etc never applied)
                if (tag == "model")
                {
                    // If it has nested element(s), serialize the first child back to XML.
                    var data = raw;

                    if (el.HasElements)
                    {
                        var first = el.Elements().FirstOrDefault();
                        if (first != null)
                            data = first.ToString(SaveOptions.DisableFormatting);
                    }

                    // Reuse the already-correct string deserializer (handles wrappers + models).
                    Deserialize(data);
                    MarkCleanIfPossible();
                    return;
                }

                // 1) LIST
                if (tag == "list" && el.HasElements)
                {
                    var obj = ParseListFromChildren(el, typeof(T));
                    if (obj is T typed)
                    {
                        Assign(typed);
                        return;
                    }

                    if (TryAssignQuiet(obj))
                        return;

                    return;
                }

                // 2) Scalar types
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

                if (tag == "double")
                {
                    Assign((T)(object)double.Parse(raw, CultureInfo.InvariantCulture));
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
                    var mb = ResolveMbObject(typeof(T), raw);
                    if (mb is T typedMb)
                    {
                        Assign(typedMb);
                        return;
                    }

                    if (typeof(T) == typeof(string))
                    {
                        Assign((T)(object)raw);
                        return;
                    }

                    MarkCleanIfPossible();
                    return;
                }

                if (tag == "enum")
                {
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

                // 3) Fallback
                var fallbackObj = ParseScalar(raw, typeof(T));
                if (fallbackObj is T fallbackTyped)
                {
                    Assign(fallbackTyped);
                    return;
                }

                MarkCleanIfPossible();
            }
            catch (Exception ex)
            {
                try
                {
                    Log.Warn($"MAttribute.DeserializeXml failed for '{el.Name}': {ex.Message}");
                }
                catch { }
            }
        }
    }
}
