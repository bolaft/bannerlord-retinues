using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using Retinues.Utilities;
using TaleWorlds.ObjectSystem;

namespace Retinues.Model
{
    public abstract partial class MBase<TBase>
        where TBase : class
    {
        const string ModelXmlVersion = "1.0";

        static readonly object CacheLock = new();
        static readonly Dictionary<Type, PropertyInfo[]> AttributePropertyCache = new();

        public string Serialize()
        {
            try
            {
                EnsureAttributesCreated();

                var root = new XElement(GetType().Name);
                root.SetAttributeValue("v", ModelXmlVersion);
                root.SetAttributeValue("type", GetType().FullName);

                if (Base is MBObjectBase mbo)
                    root.SetAttributeValue("stringId", mbo.StringId);

                foreach (var kv in _attributes)
                {
                    var attrObj = kv.Value;
                    if (attrObj == null)
                        continue;

                    if (!IsAttributeDirty(attrObj))
                        continue;

                    var el = InvokeSerializeXml(attrObj);
                    if (el != null)
                        root.Add(el);
                }

                if (!root.HasElements)
                    return string.Empty;

                return root.ToString(SaveOptions.DisableFormatting);
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to serialize MBase.");
                return string.Empty;
            }
        }

        public string Deserialize(string data)
        {
            try
            {
                EnsureAttributesCreated();

                if (string.IsNullOrWhiteSpace(data))
                    return data;

                if (data.TrimStart().StartsWith("<"))
                {
                    var doc = XDocument.Parse(data, LoadOptions.None);
                    var root = doc.Root;
                    if (root != null && (string)root.Attribute("v") == ModelXmlVersion)
                    {
                        ApplyXml(root);
                        return data;
                    }
                }

                return data;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to deserialize MBase.");
                return data;
            }
        }

        static bool IsAttributeDirty(object attrObj)
        {
            var dirtyProp = attrObj
                .GetType()
                .GetProperty("IsDirty", BindingFlags.Public | BindingFlags.Instance);

            if (dirtyProp == null)
                return true;

            try
            {
                var dirtyObj = dirtyProp.GetValue(attrObj);
                return dirtyObj is bool b && b;
            }
            catch
            {
                return true;
            }
        }

        static XElement InvokeSerializeXml(object attrObj)
        {
            var mi = attrObj
                .GetType()
                .GetMethod("SerializeXml", BindingFlags.Public | BindingFlags.Instance);

            if (mi == null)
                return null;

            try
            {
                return mi.Invoke(attrObj, null) as XElement;
            }
            catch
            {
                return null;
            }
        }

        void ApplyXml(XElement root)
        {
            var entries =
                new List<(string Name, object AttrObj, int Priority, MethodInfo Mi, XElement El)>();

            foreach (var el in root.Elements())
            {
                var name = el.Name.LocalName;

                if (!_attributes.TryGetValue(name, out var attrObj) || attrObj == null)
                    continue;

                var mi = attrObj
                    .GetType()
                    .GetMethod("DeserializeXml", BindingFlags.Public | BindingFlags.Instance);

                if (mi == null)
                    continue;

                var pr = GetPriority(attrObj);

                entries.Add((name, attrObj, pr, mi, el));
            }

            entries.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            foreach (var entry in entries)
            {
                try
                {
                    entry.Mi.Invoke(entry.AttrObj, new object[] { entry.El });
                }
                catch (Exception e)
                {
                    Log.Exception(e, $"Failed to deserialize MAttribute '{entry.Name}'.");
                }
            }
        }

        static int GetPriority(object attrObj)
        {
            var prProp = attrObj
                .GetType()
                .GetProperty("Priority", BindingFlags.Public | BindingFlags.Instance);

            if (prProp == null)
                return 0;

            try
            {
                var val = prProp.GetValue(attrObj);

                if (val is int vi)
                    return vi;

                if (val is Enum)
                    return Convert.ToInt32(val);

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        protected void EnsureAttributesCreated()
        {
            var t = GetType();

            PropertyInfo[] props;

            lock (CacheLock)
            {
                if (!AttributePropertyCache.TryGetValue(t, out props))
                {
                    props = t.GetProperties(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    AttributePropertyCache[t] = props;
                }
            }

            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];

                if (p.GetIndexParameters().Length != 0)
                    continue;

                if (!typeof(IMAttribute).IsAssignableFrom(p.PropertyType))
                    continue;

                _ = p.GetValue(this);
            }
        }
    }
}
