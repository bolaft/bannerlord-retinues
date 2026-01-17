using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Retinues.Framework.Model.Persistence;
using Retinues.Utilities;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Retinues.Framework.Model.Attributes
{
    /// <summary>
    /// File-local XML serialization helpers used by MAttribute only.
    /// This intentionally replaces the old Retinues.Utilities.Serialization.cs so the attribute system
    /// does not depend on a separate utility file.
    /// </summary>
    internal static class AttributeSerializer
    {
        /* ━━━━━━━ Public API ━━━━━━ */

        /// <summary>
        /// Serializes a value into an XML blob. XmlBlob.ToString() returns a pretty form.
        /// XmlBlob.Compact returns a compact form (recommended for persistence).
        /// </summary>
        public static XmlBlob Serialize<T>(T value)
        {
            try
            {
                var xml = SerializeToString(value, typeof(T));
                return new XmlBlob(xml);
            }
            catch
            {
                return new XmlBlob("");
            }
        }

        /// <summary>
        /// Deserializes an XML string into a value.
        /// Returns default(T) on failure or empty input.
        /// </summary>
        public static T Deserialize<T>(string xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xml))
                    return default;

                return (T)DeserializeFromString(xml, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        /* ━━━━━━━ Internals ━━━━━━ */

        static readonly XmlWriterSettings WriterSettings = new()
        {
            Indent = true,
            OmitXmlDeclaration = true,
            NewLineHandling = NewLineHandling.None,
        };

        static readonly XmlReaderSettings ReaderSettings = new()
        {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
        };

        /// <summary>
        /// Serializes an object to an XML string.
        /// </summary>
        static string SerializeToString(object value, Type declaredType)
        {
            var serializer = CreateSerializer(declaredType);

            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb, CultureInfo.InvariantCulture))
            using (var xw = XmlWriter.Create(sw, WriterSettings))
            {
                serializer.WriteObject(xw, value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Deserializes an object from an XML string.
        /// </summary>
        static object DeserializeFromString(string xml, Type declaredType)
        {
            var serializer = CreateSerializer(declaredType);

            using var sr = new StringReader(xml);
            using var xr = XmlReader.Create(sr, ReaderSettings);
            return serializer.ReadObject(xr);
        }

        /// <summary>
        /// Creates a DataContractSerializer for the given type.
        /// </summary>
        static DataContractSerializer CreateSerializer(Type type)
        {
            var settings = new DataContractSerializerSettings
            {
                MaxItemsInObjectGraph = int.MaxValue,
            };

            return new DataContractSerializer(type, settings);
        }
    }

    /// <summary>
    /// Holds serialized XML and provides pretty/compact formatting via ToString.
    /// </summary>
    internal sealed class XmlBlob(string xml)
    {
        readonly string _xml = xml ?? "";

        XDocument _doc;

        /// <summary>
        /// Parsed XDocument (lazy). Useful if you want to inspect or edit the XML.
        /// </summary>
        public XDocument Document
        {
            get
            {
                try
                {
                    if (_doc != null)
                        return _doc;

                    if (string.IsNullOrWhiteSpace(_xml))
                        return null;

                    _doc = XDocument.Parse(_xml, LoadOptions.None);
                    return _doc;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Compact XML for persistence (no whitespace formatting).
        /// </summary>
        public string Compact
        {
            get
            {
                try
                {
                    var doc = Document;
                    return doc == null ? "" : doc.ToString(SaveOptions.DisableFormatting);
                }
                catch
                {
                    return "";
                }
            }
        }
    }

    /// <summary>
    /// Serialization support for MAttribute<T>.
    /// </summary>
    public partial class MAttribute<T>
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Reflection Cache                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static readonly object CacheLock = new();

        static MethodInfo _mbGetObjectGeneric;

        static readonly Dictionary<Type, bool> IsWrapperCache = [];
        static readonly Dictionary<Type, MethodInfo> WrapperGetCache = [];

        /// <summary>
        /// Gets the generic MBObjectManager.GetObject<T>(string) method.
        /// </summary>
        static MethodInfo GetMBGetObjectGeneric()
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

        /// <summary>
        /// Determines whether the given type is a WBase<,> wrapper type.
        /// </summary>
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

        /// <summary>
        /// Gets the static Get(string) method of a WBase<,> wrapper type.
        /// </summary>
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

        /// <summary>
        /// Resolves an MBObjectBase by type and StringId.
        /// </summary>
        static MBObjectBase ResolveMBObject(Type objectType, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "null")
                return null;

            var mgr = MBObjectManager.Instance;
            if (mgr == null)
                return null;

            var getObjectGeneric = GetMBGetObjectGeneric();
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
        //             String Serialization (Compact)             //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Serializes the attribute value to a compact string representation.
        /// </summary>
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
                return AttributeSerializer.Serialize(value).Compact;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to serialize MAttribute.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Serializes an IModel instance.
        /// </summary>
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
            return AttributeSerializer.Serialize(model).Compact;
        }

        /// <summary>
        /// Serializes an enumerable collection.
        /// </summary>
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
                items.Add(AttributeSerializer.Serialize(item).Compact ?? string.Empty);
            }

            return AttributeSerializer.Serialize(items).Compact;
        }

        /// <summary>
        /// Deserializes the attribute value from a compact string representation.
        /// </summary>
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
                    var obj = ResolveMBObject(t, data);
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
                var deserialized = AttributeSerializer.Deserialize<T>(data);
                SetValue(deserialized, markDirty: true);
                return data;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to deserialize MAttribute.");
                return data;
            }
        }

        /// <summary>
        /// Deserializes a single WBase<,> wrapper from its StringId.
        /// </summary>
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

        /// <summary>
        /// Deserializes a List<MBObjectBase> from a list of StringIds.
        /// </summary>
        void DeserializeMbObjectList(Type elemType, string data)
        {
            var ids = AttributeSerializer.Deserialize<List<string>>(data) ?? [];
            var listType = typeof(List<>).MakeGenericType(elemType);
            var list = (IList)Activator.CreateInstance(listType);

            foreach (var id in ids)
            {
                var obj = ResolveMBObject(elemType, id);
                list.Add(obj);
            }

            SetValue((T)list, markDirty: true);
        }

        /// <summary>
        /// Deserializes a List<WBase<,>> from a list of StringIds.
        /// </summary>
        void DeserializeWrapperList(Type elemType, string data)
        {
            try
            {
                var ids = AttributeSerializer.Deserialize<List<string>>(data) ?? [];

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

        /// <summary>
        /// Tries to get the value type of a Dictionary with string keys.
        /// </summary>
        static bool TryGetStringKeyedDictionaryValueType(Type t, out Type valueType)
        {
            valueType = null;

            if (t == null || !t.IsGenericType)
                return false;

            if (t.GetGenericTypeDefinition() != typeof(Dictionary<,>))
                return false;

            var args = t.GetGenericArguments();
            if (args.Length != 2)
                return false;

            if (args[0] != typeof(string))
                return false;

            valueType = args[1];
            return true;
        }

        /// <summary>
        /// Serializes a scalar-like object to string.
        /// </summary>
        static string SerializeScalarLike(object obj, Type targetType)
        {
            if (obj == null)
                return string.Empty;

            if (targetType == typeof(string))
                return (string)obj;

            if (targetType == typeof(TextObject))
                return ((TextObject)obj)?.Value ?? string.Empty;

            if (targetType == typeof(bool))
                return ((bool)obj) ? "true" : "false";

            if (targetType == typeof(int))
                return ((int)obj).ToString(Inv);

            if (targetType == typeof(float))
                return ((float)obj).ToString("R", Inv);

            if (targetType == typeof(double))
                return ((double)obj).ToString("R", Inv);

            if (targetType.IsEnum)
                return obj.ToString();

            if (typeof(MBObjectBase).IsAssignableFrom(targetType))
                return ((MBObjectBase)obj)?.StringId ?? string.Empty;

            return Convert.ToString(obj, Inv) ?? string.Empty;
        }
    }
}
