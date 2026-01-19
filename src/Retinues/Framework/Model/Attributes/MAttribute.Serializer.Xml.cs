using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Retinues.Framework.Model.Persistence;
using Retinues.Utilities;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Retinues.Framework.Model.Attributes
{
    /// <summary>
    /// Serialization support for MAttribute<T>.
    /// </summary>
    public partial class MAttribute<T>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //            XML Element Serialization (MBase)           //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Serializes the attribute value to an XElement.
        /// </summary>
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

            if (
                value is IDictionary dict
                && TryGetStringKeyedDictionaryValueType(typeof(T), out var valueType)
            )
            {
                el.SetAttributeValue("t", "map");

                foreach (DictionaryEntry entry in dict)
                {
                    var k = entry.Key?.ToString() ?? string.Empty;
                    var v = SerializeScalarLike(entry.Value, valueType);

                    el.Add(new XElement("E", new XAttribute("k", k), new XAttribute("v", v)));
                }

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

        /// <summary>
        /// Deserializes the attribute value from an XElement.
        /// </summary>
        public void DeserializeXml(XElement el)
        {
            if (el == null)
                return;

            var tag = ((string)el.Attribute("t") ?? string.Empty).Trim();

            var raw = (string)el.Attribute("v") ?? el.Value;
            raw = (raw ?? string.Empty).Trim();

            bool restoring = MBase<IModel>.IsRestoringFromPersistence;

            /// <summary>
            /// Marks the attribute as clean if not restoring.
            /// </summary>
            void MarkCleanIfAllowed()
            {
                if (restoring)
                    return;

                try
                {
                    if (Reflection.HasField(this, "_dirty"))
                        Reflection.SetFieldValue(this, "_dirty", false);

                    try
                    {
                        Reflection.InvokeMethod(this, "MarkClean", null);
                    }
                    catch { }
                }
                catch { }
            }

            /// <summary>
            /// Cleans a string for logging during load.
            /// </summary>
            string CleanForLoadLog(string s)
            {
                s ??= string.Empty;

                s = s.Replace("\r", " ").Replace("\n", " ");
                s = s.Replace("|", "/");

                s = s.Trim();

                const int max = 140;
                if (s.Length > max)
                    s =
                        s.Substring(0, max)
                        + "...("
                        + s.Length.ToString(CultureInfo.InvariantCulture)
                        + ")";

                return s;
            }

            /// <summary>
            /// Previews a list XElement for logging.
            /// </summary>
            string PreviewList(XElement listEl)
            {
                if (listEl == null)
                    return "list[0]";

                int count = 0;
                var items = new List<string>();

                foreach (var child in listEl.Elements())
                {
                    count++;

                    if (items.Count >= 3)
                        continue;

                    if (child.Name.LocalName == "Item")
                    {
                        if (child.HasElements)
                        {
                            var first = child.Elements().FirstOrDefault();
                            items.Add(first == null ? "" : first.Name.LocalName);
                        }
                        else
                        {
                            items.Add((child.Value ?? string.Empty).Trim());
                        }
                    }
                    else
                    {
                        items.Add(child.Name.LocalName);
                    }
                }

                var preview =
                    items.Count == 0
                        ? ""
                        : "(" + string.Join(",", items.Select(CleanForLoadLog)) + ")";
                return "list[" + count.ToString(CultureInfo.InvariantCulture) + "]" + preview;
            }

            /// <summary>
            /// Previews a map XElement for logging.
            /// </summary>
            string PreviewMap(XElement mapEl)
            {
                if (mapEl == null)
                    return "map[0]";

                var entries = mapEl.Elements("E").ToList();
                var preview = new List<string>();

                for (int i = 0; i < entries.Count && i < 3; i++)
                {
                    var e = entries[i];
                    var k = ((string)e.Attribute("k") ?? string.Empty).Trim();
                    var v = ((string)e.Attribute("v") ?? string.Empty).Trim();
                    preview.Add(CleanForLoadLog(k) + "=" + CleanForLoadLog(v));
                }

                var p = preview.Count == 0 ? "" : "(" + string.Join(",", preview) + ")";
                return "map[" + entries.Count.ToString(CultureInfo.InvariantCulture) + "]" + p;
            }

            /// <summary>
            /// Assigns the parsed value and logs it.
            /// </summary>
            void Assign(T value)
            {
                try
                {
                    Set(value);
                    MarkCleanIfAllowed();

                    // Prefer compact, stable representations based on XML tag.
                    string formatted;

                    if (tag == "list")
                        formatted = PreviewList(el);
                    else if (tag == "map")
                        formatted = PreviewMap(el);
                    else if (tag == "mb")
                        formatted = CleanForLoadLog(raw);
                    else if (
                        tag == "text"
                        || tag == "string"
                        || tag == "enum"
                        || tag == "bool"
                        || tag == "int"
                        || tag == "float"
                        || tag == "double"
                    )
                        formatted = CleanForLoadLog(raw);
                    else
                        formatted = CleanForLoadLog(value == null ? "" : value.ToString());

                    LoadingLogger.Add(Name, formatted);
                }
                catch
                {
                    try
                    {
                        if (Reflection.HasField(this, "_value"))
                        {
                            Reflection.SetFieldValue(this, "_value", value);
                            MarkCleanIfAllowed();
                        }
                    }
                    catch { }
                }
            }

            /// <summary>
            /// Parses a scalar value from string.
            /// </summary>
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
                    return ResolveMBObject(targetType, s);

                try
                {
                    return Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return null;
                }
            }

            /// <summary>
            /// Parses a list from child elements.
            /// </summary>
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
                                itemObj = ResolveMBObject(itemType, v);
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
                // MODEL
                if (tag == "model")
                {
                    var data = raw;

                    if (el.HasElements)
                    {
                        var first = el.Elements().FirstOrDefault();
                        if (first != null)
                            data = first.ToString(SaveOptions.DisableFormatting);
                    }

                    Deserialize(data);
                    MarkCleanIfAllowed();
                    return;
                }

                // LIST
                if (tag == "list")
                {
                    var obj = ParseListFromChildren(el, typeof(T));
                    if (obj is T typed)
                    {
                        Assign(typed);
                        return;
                    }

                    return;
                }

                // SCALARS
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

                if (tag == "enum")
                {
                    Assign((T)Enum.Parse(typeof(T), raw, true));
                    return;
                }

                if (tag == "map")
                {
                    if (TryGetStringKeyedDictionaryValueType(typeof(T), out var valueType))
                    {
                        var dictType = typeof(Dictionary<,>).MakeGenericType(
                            typeof(string),
                            valueType
                        );
                        var result = (IDictionary)Activator.CreateInstance(dictType);

                        foreach (var e in el.Elements("E"))
                        {
                            var k = ((string)e.Attribute("k") ?? string.Empty).Trim();
                            var vRaw = ((string)e.Attribute("v") ?? string.Empty).Trim();

                            // ParseScalar already exists in DeserializeXml
                            var vObj = ParseScalar(vRaw, valueType);

                            result[k] = vObj;
                        }

                        Assign((T)(object)result);
                    }

                    return;
                }

                if (tag == "list")
                {
                    var obj = ParseListFromChildren(el, typeof(T));
                    if (obj is T typed)
                    {
                        Assign(typed);
                        return;
                    }

                    return;
                }

                if (tag == "mb")
                {
                    var mb = ResolveMBObject(typeof(T), raw);
                    if (mb is T typed)
                        Assign(typed);
                    return;
                }

                // FALLBACK
                var fallback = ParseScalar(raw, typeof(T));
                if (fallback is T f)
                    Assign(f);
            }
            catch (Exception ex)
            {
                Log.Warning($"MAttribute.DeserializeXml failed for '{Name}': {ex.Message}");
            }
        }
    }
}
