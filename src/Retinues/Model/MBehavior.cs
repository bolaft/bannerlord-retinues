using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Retinues.Behaviors;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;

namespace Retinues.Model
{
    /// <summary>
    /// Handles saving and loading of persistent wrapper data into the game save.
    /// </summary>
    public sealed partial class MBehavior : BaseCampaignBehavior
    {
        const string SaveKey = "Retinues_ModelPersistence";
        const string RootName = "Retinues";
        const string RootVersion = "2";

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore == null)
                return;

            try
            {
                string blob = string.Empty;

                if (dataStore.IsSaving)
                    blob = BuildSaveBlob();

                dataStore.SyncData(SaveKey, ref blob);

                if (!string.IsNullOrEmpty(blob))
                    Log.Info(
                        $"MPersistenceBehavior.SyncData: SyncData ok for key '{SaveKey}'. blob length={(blob == null ? 0 : blob.Length)}"
                    );

                if (!dataStore.IsLoading)
                    return;

                if (string.IsNullOrEmpty(blob))
                    return;

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
            }
            catch (Exception e)
            {
                Log.Exception(e, "MPersistenceBehavior.SyncData failed");
            }
        }

        static string BuildSaveBlob()
        {
            var root = new XElement(RootName);
            root.SetAttributeValue("v", RootVersion);

            var seen = new HashSet<string>(StringComparer.Ordinal);

            var asm = typeof(MBehavior).Assembly;
            foreach (var t in asm.GetTypes())
            {
                if (!WrapperReflection.IsConcreteWrapperType(t))
                    continue;

                var all = WrapperReflection.TryGetAllEnumerable(t);
                if (all == null)
                    continue;

                foreach (var inst in all)
                {
                    if (inst == null)
                        continue;

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

        static void TryWriteBackupFile(XElement root)
        {
            try
            {
                var blob = root.ToString(SaveOptions.DisableFormatting);
                if (string.IsNullOrWhiteSpace(blob))
                    return;

                var filePath = FileSystem.GetPathInRetinuesDocuments("Backups", "BackupExport.xml");
                var fileContent = root.ToString(SaveOptions.None);

                File.WriteAllText(filePath, fileContent, Encoding.UTF8);
                Log.Info(
                    $"MPersistenceBehavior.SyncData: wrote persistence XML to '{filePath}' (length={fileContent.Length})."
                );
            }
            catch (Exception e)
            {
                Log.Warn($"MPersistenceBehavior.SyncData: failed to write persistence XML: {e}");
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
    }
}
