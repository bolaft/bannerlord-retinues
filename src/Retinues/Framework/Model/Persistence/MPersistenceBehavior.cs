using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Retinues.Framework.Behaviors;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Framework.Model.Persistence
{
    /// <summary>
    /// Handles saving and loading of persistent wrapper data into the game save.
    /// </summary>
    public sealed partial class MPersistenceBehavior : BaseCampaignBehavior
    {
        const string SaveKey = "Retinues_ModelPersistence";
        const string RootName = "Retinues";
        const string RootVersion = "1";

        /// <summary>
        /// Synchronizes persistent data to/from the save.
        /// </summary>
        public override void SyncData(IDataStore dataStore)
        {
            Log.Info("MPersistenceBehavior.SyncData called.");
            if (dataStore == null)
                return;

            try
            {
                const string V2CountKey = SaveKey + "_v2_count";
                const string V2PartKeyPrefix = SaveKey + "_v2_part_";

                // Base64 is ASCII, so chars == bytes for the stored payload.
                // Keep a healthy margin under any per-entry limit.
                const int MaxPartChars = 24000;

                if (dataStore.IsSaving)
                {
                    var xml = BuildSaveBlob();

                    // Pack (gzip + base64) then chunk into multiple small entries.
                    var packed = PackToBase64Gzip(xml);
                    var parts = SplitIntoParts(packed, MaxPartChars);

                    var count = parts.Count;
                    dataStore.SyncData(V2CountKey, ref count);

                    for (int i = 0; i < parts.Count; i++)
                    {
                        var part = parts[i] ?? string.Empty;
                        dataStore.SyncData(V2PartKeyPrefix + i, ref part);
                    }

                    Log.Info(
                        $"MPersistenceBehavior.SyncData: Saving v2 chunks. xmlChars={xml.Length} packedChars={packed.Length} parts={parts.Count}"
                    );

                    return;
                }

                // Loading
                var v2Count = 0;
                dataStore.SyncData(V2CountKey, ref v2Count);

                string xmlBlob = null;

                if (v2Count > 0)
                {
                    var sb = new StringBuilder(v2Count * MaxPartChars);

                    for (int i = 0; i < v2Count; i++)
                    {
                        var part = string.Empty;
                        dataStore.SyncData(V2PartKeyPrefix + i, ref part);
                        sb.Append(part ?? string.Empty);
                    }

                    var packed = sb.ToString();
                    xmlBlob = UnpackFromBase64Gzip(packed);

                    Log.Info(
                        $"MPersistenceBehavior.SyncData: Loaded v2 chunks. packedChars={packed.Length} xmlChars={(xmlBlob != null ? xmlBlob.Length : 0)} parts={v2Count}"
                    );
                }
                else
                {
                    Log.Info("MPersistenceBehavior.SyncData: No v2 chunks found.");
                    return;
                }

                if (string.IsNullOrEmpty(xmlBlob))
                    return;

                if (!TryParseXmlRoot(xmlBlob, out var xmlRoot))
                    return;

                if (xmlRoot.Name.LocalName != RootName)
                    return;

                var v = (string)xmlRoot.Attribute("v");
                if (v != RootVersion)
                    return;

                ApplyXml(xmlRoot);
            }
            catch (Exception e)
            {
                Log.Exception(e, "MPersistenceBehavior.SyncData failed");
            }
        }

        /// <summary>
        /// Packs a string into a base64-gzip representation.
        /// </summary>
        static string PackToBase64Gzip(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            var input = Encoding.UTF8.GetBytes(s);

            using var ms = new MemoryStream();
            using (var gz = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            {
                gz.Write(input, 0, input.Length);
            }

            var compressed = ms.ToArray();
            return Convert.ToBase64String(compressed);
        }

        /// <summary>
        /// Unpacks a base64-gzip string into its original representation.
        /// </summary>
        static string UnpackFromBase64Gzip(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return string.Empty;

            byte[] compressed;

            try
            {
                compressed = Convert.FromBase64String(base64);
            }
            catch
            {
                return string.Empty;
            }

            using var input = new MemoryStream(compressed);
            using var gz = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            gz.CopyTo(output);

            var bytes = output.ToArray();
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Splits a string into parts of maximum length.
        /// </summary>
        static List<string> SplitIntoParts(string s, int maxLen)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(s))
                return list;

            if (maxLen <= 0)
            {
                list.Add(s);
                return list;
            }

            for (int i = 0; i < s.Length; i += maxLen)
            {
                var len = Math.Min(maxLen, s.Length - i);
                list.Add(s.Substring(i, len));
            }

            return list;
        }

        /// <summary>
        /// Builds the save blob XML string.
        /// </summary>
        static string BuildSaveBlob()
        {
            var root = new XElement(RootName)
            {
                // keep compact output
            };
            root.SetAttributeValue("v", RootVersion);

            var seen = new HashSet<string>(StringComparer.Ordinal);

            // Resolve MBObjectManager.GetObjectTypeList<T>() once (reflection)
            var mgr = TaleWorlds.ObjectSystem.MBObjectManager.Instance;
            var getObjectTypeListGeneric = typeof(TaleWorlds.ObjectSystem.MBObjectManager)
                .GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                )
                .FirstOrDefault(m =>
                    m.Name == "GetObjectTypeList"
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 0
                );

            /// <summary>
            /// Enumerates all instances of the given wrapper type.
            /// </summary>
            IEnumerable EnumerateInstances(Type wrapperType)
            {
                // If the wrapper declares its own All, do NOT execute it
                // (it may depend on Campaign.Current or be accidentally recursive).
                var allProp = wrapperType.GetProperty(
                    "All",
                    System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.Static
                        | System.Reflection.BindingFlags.DeclaredOnly
                );

                var declaresAll = allProp != null;

                if (declaresAll && mgr != null && getObjectTypeListGeneric != null)
                {
                    var wb = WrapperReflection.GetWBaseGeneric(wrapperType);
                    if (wb != null)
                    {
                        var baseArg = wb.GetGenericArguments()[1];

                        if (
                            baseArg != null
                            && typeof(TaleWorlds.ObjectSystem.MBObjectBase).IsAssignableFrom(
                                baseArg
                            )
                        )
                        {
                            // Prefer Get(TBase) instead of Get(string) to avoid custom Get(string)->All recursion.
                            var getByBase = wrapperType.GetMethod(
                                "Get",
                                System.Reflection.BindingFlags.Public
                                    | System.Reflection.BindingFlags.Static
                                    | System.Reflection.BindingFlags.FlattenHierarchy,
                                binder: null,
                                types: [baseArg],
                                modifiers: null
                            );

                            if (getByBase != null)
                            {
                                object baseListObj = null;
                                try
                                {
                                    baseListObj = getObjectTypeListGeneric
                                        .MakeGenericMethod(baseArg)
                                        .Invoke(mgr, null);
                                }
                                catch
                                {
                                    baseListObj = null;
                                }

                                if (baseListObj is IEnumerable baseList)
                                {
                                    foreach (var mbo in baseList)
                                    {
                                        if (mbo == null)
                                            continue;

                                        object w = null;
                                        try
                                        {
                                            w = getByBase.Invoke(null, [mbo]);
                                        }
                                        catch
                                        {
                                            w = null;
                                        }

                                        if (w != null)
                                            yield return w;
                                    }

                                    yield break;
                                }
                            }
                        }
                    }
                }

                // Fallback: original path (may execute wrapper.All)
                var all = WrapperReflection.TryGetAllEnumerable(wrapperType);
                if (all == null)
                    yield break;

                foreach (var inst in all)
                {
                    if (inst != null)
                        yield return inst;
                }
            }

            var asm = typeof(MPersistenceBehavior).Assembly;
            foreach (var t in asm.GetTypes())
            {
                if (!WrapperReflection.IsConcreteWrapperType(t))
                    continue;

                foreach (var inst in EnumerateInstances(t))
                {
                    var uid = WrapperReflection.TryGetUniqueId(inst);
                    if (string.IsNullOrEmpty(uid))
                        continue;

                    if (seen.Contains(uid))
                        continue;

                    var serialized = WrapperReflection.TrySerialize(inst);
                    if (string.IsNullOrEmpty(serialized))
                        continue;

                    seen.Add(uid);
                    AddEntry(root, uid, serialized);
                }
            }

            var blob = root.ToString(SaveOptions.DisableFormatting);
            TryWriteBackupFile(root);
            return blob;
        }

        /// <summary>
        /// Adds a serialized persistence entry to the given root.
        /// </summary>
        static void AddEntry(XElement root, string uid, string serialized)
        {
            var trimmed = serialized.TrimStart();

            if (trimmed.StartsWith("<"))
            {
                try
                {
                    var el = XElement.Parse(serialized, LoadOptions.None);
                    el.SetAttributeValue("uid", uid);
                    root.Add(el);
                    return;
                }
                catch { }
            }

            root.Add(
                new XElement(
                    "Entry",
                    new XAttribute("uid", uid),
                    new XAttribute("format", "text"),
                    new XCData(serialized)
                )
            );
        }

        /// <summary>
        /// Tries to write a backup file of the persistence XML for debugging.
        /// </summary>
        static void TryWriteBackupFile(XElement root)
        {
            try
            {
                var blob = root.ToString(SaveOptions.DisableFormatting);
                if (string.IsNullOrWhiteSpace(blob))
                    return;

                var filePath = FileSystem.GetPathInRetinuesDocuments(
                    "Snapshots",
                    "LastSaveSnapshot.xml"
                );
                var fileContent = root.ToString(SaveOptions.None);

                File.WriteAllText(filePath, fileContent, Encoding.UTF8);
                Log.Debug(
                    $"MPersistenceBehavior.SyncData: wrote persistence XML to '{filePath}' (length={fileContent.Length})."
                );
            }
            catch (Exception e)
            {
                Log.Warning($"MPersistenceBehavior.SyncData: failed to write persistence XML: {e}");
            }
        }

        /// <summary>
        /// Tries to parse an XML root from the given string.
        /// </summary>
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
    }
}
