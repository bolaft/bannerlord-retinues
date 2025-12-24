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

                    // Skip non-dirty attributes (same behavior as today)
                    var dirtyProp = attrObj
                        .GetType()
                        .GetProperty("IsDirty", BindingFlags.Public | BindingFlags.Instance);
                    if (dirtyProp != null)
                    {
                        var dirtyObj = dirtyProp.GetValue(attrObj);
                        var isDirty = dirtyObj is bool b && b;
                        if (!isDirty)
                            continue;
                    }

                    var mi = attrObj
                        .GetType()
                        .GetMethod("SerializeXml", BindingFlags.Public | BindingFlags.Instance);
                    if (mi == null)
                        continue;

                    var el = mi.Invoke(attrObj, null) as XElement;
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

                // New format
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

        void ApplyXml(XElement root)
        {
            // Same priority ordering as your current code
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

                var prProp = attrObj
                    .GetType()
                    .GetProperty("Priority", BindingFlags.Public | BindingFlags.Instance);
                var pr = 0;
                if (prProp != null)
                {
                    var val = prProp.GetValue(attrObj);
                    if (val is int vi)
                        pr = vi;
                    else if (val is Enum)
                        pr = Convert.ToInt32(val);
                }

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

        protected void EnsureAttributesCreated()
        {
            var props = GetType()
                .GetProperties(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );
            foreach (var p in props)
            {
                if (p.GetIndexParameters().Length != 0)
                    continue;

                if (!typeof(IMAttribute).IsAssignableFrom(p.PropertyType))
                    continue;

                _ = p.GetValue(this);
            }
        }
    }
}
