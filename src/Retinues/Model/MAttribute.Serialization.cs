using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Retinues.Utilities;
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
            try
            {
                if (el == null)
                {
                    SetValue(default, markDirty: true);
                    return;
                }

                var t = typeof(T);
                var text = el.Value ?? string.Empty;

                // MBObjectBase stored as StringId
                if (typeof(MBObjectBase).IsAssignableFrom(t))
                {
                    Deserialize(text);
                    return;
                }

                // WBase wrapper stored as StringId
                if (IsWBaseType(t))
                {
                    DeserializeSingleWrapper(t, text);
                    return;
                }

                // List<T>
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elemType = t.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elemType);
                    var list = (IList)Activator.CreateInstance(listType);

                    var items = el.Elements("Item").ToList();
                    if (items.Count == 0)
                        items = [.. el.Elements()];

                    foreach (var itemEl in items)
                    {
                        string itemText;

                        if (itemEl.Name.LocalName != "Item" && itemEl.HasElements)
                            itemText = itemEl.ToString(SaveOptions.DisableFormatting);
                        else
                            itemText = itemEl.Value ?? string.Empty;

                        if (typeof(MBObjectBase).IsAssignableFrom(elemType))
                        {
                            list.Add(ResolveMbObject(elemType, itemText));
                            continue;
                        }

                        if (IsWBaseType(elemType))
                        {
                            var get = GetWrapperGetMethod(elemType);
                            var w = get?.Invoke(null, [itemText]);
                            if (w != null)
                                list.Add(w);
                            continue;
                        }

                        if (elemType == typeof(string))
                        {
                            list.Add(itemText);
                            continue;
                        }

                        if (
                            elemType == typeof(int)
                            && int.TryParse(itemText, NumberStyles.Integer, Inv, out var li)
                        )
                        {
                            list.Add(li);
                            continue;
                        }

                        if (elemType == typeof(bool) && bool.TryParse(itemText, out var lb))
                        {
                            list.Add(lb);
                            continue;
                        }

                        if (
                            elemType == typeof(float)
                            && float.TryParse(itemText, NumberStyles.Float, Inv, out var lf)
                        )
                        {
                            list.Add(lf);
                            continue;
                        }

                        list.Add(itemText);
                    }

                    SetValue((T)list, markDirty: true);
                    return;
                }

                // TextObject (store only Value)
                if (t == typeof(TextObject))
                {
                    SetValue((T)(object)new TextObject(text), markDirty: true);
                    return;
                }

                // Primitives / strings
                if (t == typeof(string))
                {
                    SetValue((T)(object)text, markDirty: true);
                    return;
                }

                if (t == typeof(int) && int.TryParse(text, NumberStyles.Integer, Inv, out var i))
                {
                    SetValue((T)(object)i, markDirty: true);
                    return;
                }

                if (t == typeof(bool) && bool.TryParse(text, out var b))
                {
                    SetValue((T)(object)b, markDirty: true);
                    return;
                }

                if (t == typeof(float) && float.TryParse(text, NumberStyles.Float, Inv, out var f))
                {
                    SetValue((T)(object)f, markDirty: true);
                    return;
                }

                if (
                    t == typeof(double)
                    && double.TryParse(text, NumberStyles.Float, Inv, out var d)
                )
                {
                    SetValue((T)(object)d, markDirty: true);
                    return;
                }

                // Non-WBase model: if we embedded XML, let the existing instance handle it
                if (typeof(IModel).IsAssignableFrom(t) && !IsWBaseType(t))
                {
                    var current = Get();
                    if (current != null)
                    {
                        var innerModelEl = el.Elements().FirstOrDefault();
                        if (innerModelEl != null)
                        {
                            var mi = current
                                .GetType()
                                .GetMethod(
                                    "Deserialize",
                                    BindingFlags.Public | BindingFlags.Instance
                                );

                            mi?.Invoke(
                                current,
                                [innerModelEl.ToString(SaveOptions.DisableFormatting)]
                            );

                            return;
                        }
                    }
                }

                // Final fallback: try existing Deserialize(string) path
                Deserialize(text);
            }
            catch
            {
                SetValue(default, markDirty: true);
            }
        }
    }
}
