using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Retinues.Behaviors;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Model
{
    /// <summary>
    /// Handles saving and loading of persistent wrapper data into the game save.
    ///
    /// v2 format (preferred):
    /// <RetinuesPersistence v="2">
    ///   <WCharacter uid="TaleWorlds.CampaignSystem.CharacterObject:aserai_youth" ...>...</WCharacter>
    ///   <WItem uid="TaleWorlds.Core.ItemObject:some_item" ...>...</WItem>
    ///   ...
    /// </RetinuesPersistence>
    ///
    /// Backward compatibility:
    /// - Still supports legacy Dictionary<string,string> persisted via DataContractSerializer
    ///   (the ugly ArrayOfKeyValueOfstringstring format).
    /// </summary>
    public sealed class MPersistenceBehavior : BaseCampaignBehavior
    {
        const string SaveKey = "Retinues_ModelPersistence";

        const string RootName = "RetinuesPersistence";
        const string RootVersion = "2";

        public MPersistenceBehavior() { }

        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore == null)
                return;

            try
            {
                string blob = string.Empty;

                // Saving: build the blob first.
                if (dataStore.IsSaving)
                {
                    var root = new XElement(RootName);
                    root.SetAttributeValue("v", RootVersion);

                    var seen = new HashSet<string>(StringComparer.Ordinal);

                    var asm = typeof(MPersistenceBehavior).Assembly;
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.IsAbstract || !t.IsClass)
                            continue;

                        var wb = GetWBaseGeneric(t);
                        if (wb == null)
                            continue;

                        // 'All' is defined on WBase<,> (single authoritative definition)
                        var allProp = wb.GetProperty(
                            "All",
                            BindingFlags.Public | BindingFlags.Static
                        );

                        if (allProp == null)
                            continue;

                        var allObj = allProp.GetValue(null);
                        if (allObj is not System.Collections.IEnumerable enumerable)
                            continue;

                        foreach (var inst in enumerable)
                        {
                            if (inst == null)
                                continue;

                            var uidProp = inst.GetType()
                                .GetProperty(
                                    "UniqueId",
                                    BindingFlags.Public
                                        | BindingFlags.Instance
                                        | BindingFlags.FlattenHierarchy
                                );

                            var serializeMi = inst.GetType()
                                .GetMethod(
                                    "Serialize",
                                    BindingFlags.Public
                                        | BindingFlags.Instance
                                        | BindingFlags.FlattenHierarchy
                                );

                            if (uidProp == null || serializeMi == null)
                                continue;

                            var uid = uidProp.GetValue(inst, null) as string;
                            if (string.IsNullOrEmpty(uid))
                                continue;

                            // Avoid duplicates
                            if (seen.Contains(uid))
                                continue;

                            var serialized = serializeMi.Invoke(inst, null) as string;
                            if (string.IsNullOrEmpty(serialized))
                                continue;

                            seen.Add(uid);

                            // If wrapper serialization is XML (new MBase v2), embed as XML (pretty).
                            // Otherwise store as CDATA under <Entry>.
                            var trimmed = serialized.TrimStart();
                            if (trimmed.StartsWith("<"))
                            {
                                try
                                {
                                    var el = XElement.Parse(serialized, LoadOptions.None);

                                    // Attach uid at persistence layer.
                                    el.SetAttributeValue("uid", uid);

                                    root.Add(el);
                                }
                                catch
                                {
                                    root.Add(
                                        new XElement(
                                            "Entry",
                                            new XAttribute("uid", uid),
                                            new XAttribute("format", "text"),
                                            new XCData(serialized)
                                        )
                                    );
                                }
                            }
                            else
                            {
                                root.Add(
                                    new XElement(
                                        "Entry",
                                        new XAttribute("uid", uid),
                                        new XAttribute("format", "text"),
                                        new XCData(serialized)
                                    )
                                );
                            }
                        }
                    }

                    // Compact in save.
                    blob = root.ToString(SaveOptions.DisableFormatting);

                    // Optional debug write next to the game executable.
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(blob))
                        {
                            var baseDir =
                                AppDomain.CurrentDomain.BaseDirectory
                                ?? Environment.CurrentDirectory;
                            var filePath = Path.Combine(baseDir, "Retinues_ModelPersistence.xml");

                            // Pretty file for humans.
                            var fileContent = root.ToString(SaveOptions.None);

                            File.WriteAllText(filePath, fileContent, Encoding.UTF8);
                            Log.Info(
                                $"MPersistenceBehavior.SyncData: wrote persistence XML to '{filePath}' (length={fileContent.Length})."
                            );
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warn(
                            $"MPersistenceBehavior.SyncData: failed to write persistence XML: {e}"
                        );
                    }
                }

                // Single SyncData call per key (critical: no duplicate-key exceptions).
                dataStore.SyncData(SaveKey, ref blob);

                if (!string.IsNullOrEmpty(blob))
                    Log.Info(
                        $"MPersistenceBehavior.SyncData: SyncData ok for key '{SaveKey}'. blob length={(blob == null ? 0 : blob.Length)}"
                    );

                if (!dataStore.IsLoading)
                    return;

                if (string.IsNullOrEmpty(blob))
                    return;

                // APPLY (v2 XML first, fallback to legacy dictionary)
                if (TryParseXmlRoot(blob, out var xmlRoot))
                {
                    if (xmlRoot.Name.LocalName == RootName)
                    {
                        var v = (string)xmlRoot.Attribute("v");
                        if (v == RootVersion)
                        {
                            ApplyXml(xmlRoot);
                            return;
                        }
                    }
                }

                // Legacy fallback: Dictionary<string,string> serialized via DataContractSerializer.
                try
                {
                    var loaded = Serialization.Deserialize<Dictionary<string, string>>(blob);
                    ApplyLoadedLegacy(loaded);
                }
                catch (Exception e)
                {
                    Log.Exception(
                        e,
                        "MPersistenceBehavior.SyncData: failed to load legacy persistence blob"
                    );
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, "MPersistenceBehavior.SyncData failed");
            }
        }

        static bool TryParseXmlRoot(string xml, out XElement root)
        {
            root = null;

            if (string.IsNullOrWhiteSpace(xml))
                return false;

            var trimmed = xml.TrimStart();
            if (!trimmed.StartsWith("<"))
                return false;

            try
            {
                var doc = XDocument.Parse(xml, LoadOptions.None);
                root = doc.Root;
                return root != null;
            }
            catch
            {
                return false;
            }
        }

        static readonly Dictionary<string, Type> WrapperByBaseFullName = BuildWrapperTypeMap();

        static Dictionary<string, Type> BuildWrapperTypeMap()
        {
            var asm = typeof(MPersistenceBehavior).Assembly;
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);

            foreach (var t in asm.GetTypes())
            {
                if (!t.IsClass || t.IsAbstract)
                    continue;

                // Walk base types to find WBase<,>
                var bt = t.BaseType;
                while (
                    bt != null
                    && (!bt.IsGenericType || bt.GetGenericTypeDefinition() != typeof(WBase<,>))
                )
                    bt = bt.BaseType;

                if (bt == null)
                    continue;

                var baseArg = bt.GetGenericArguments()[1]; // TBase
                if (baseArg != null && baseArg.FullName != null)
                    map[baseArg.FullName] = t;
            }

            return map;
        }

        static void ApplyXml(XElement root)
        {
            foreach (var el in root.Elements())
            {
                // Two supported shapes:
                // 1) <SomeWrapper uid="BaseFullName:StringId" ...>...</SomeWrapper>
                // 2) <Entry uid="BaseFullName:StringId" format="text"><![CDATA[...]]></Entry>
                var uid = (string)el.Attribute("uid");
                if (string.IsNullOrEmpty(uid))
                    continue;

                string payload;

                if (el.Name.LocalName == "Entry")
                {
                    // Text payload in CDATA (or Value).
                    payload = el.Value ?? string.Empty;
                }
                else
                {
                    // Wrapper element payload is itself XML.
                    // Remove the persistence-layer uid attribute to avoid strict deserializers choking on unknown attrs.
                    var copy = new XElement(el);
                    copy.SetAttributeValue("uid", null);
                    payload = copy.ToString(SaveOptions.DisableFormatting);
                }

                ApplySingle(uid, payload);
            }
        }

        static void ApplyLoadedLegacy(Dictionary<string, string> loaded)
        {
            if (loaded == null || loaded.Count == 0)
                return;

            foreach (var kv in loaded)
                ApplySingle(kv.Key, kv.Value);
        }

        static void ApplySingle(string uid, string data)
        {
            if (string.IsNullOrEmpty(uid))
                return;

            var sep = uid.IndexOf(':');
            if (sep <= 0 || sep >= uid.Length - 1)
                return;

            var baseTypeFullName = uid.Substring(0, sep);
            var stringId = uid.Substring(sep + 1);

            if (!WrapperByBaseFullName.TryGetValue(baseTypeFullName, out var wrapperType))
                return;

            var get = wrapperType.GetMethod(
                "Get",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                null,
                new[] { typeof(string) },
                null
            );

            if (get == null)
                return;

            var wrapper = get.Invoke(null, new object[] { stringId });
            if (wrapper == null)
                return;

            var deserialize = wrapperType.GetMethod(
                "Deserialize",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                null,
                new[] { typeof(string) },
                null
            );

            if (deserialize == null)
                return;

            deserialize.Invoke(wrapper, new object[] { data });
        }

        static Type GetWBaseGeneric(Type t)
        {
            // Walk base types until we find WBase<,>
            var bt = t;
            while (bt != null)
            {
                if (bt.IsGenericType && bt.GetGenericTypeDefinition() == typeof(WBase<,>))
                    return bt;

                bt = bt.BaseType;
            }

            return null;
        }
    }
}
