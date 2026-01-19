using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Retinues.Utilities;
using TaleWorlds.ObjectSystem;

namespace Retinues.Framework.Model.Attributes
{
    /// <summary>
    /// Serialization support for MAttribute<T>.
    /// </summary>
    public partial class MAttribute<T>
    {
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
    }
}
