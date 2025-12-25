using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Retinues.Model.Characters;
using Retinues.Utilities;

namespace Retinues.Model
{
    public static partial class MLibrary
    {
        public sealed partial class Item
        {
            /// <summary>
            /// Leases a temporary WCharacter stub and applies this library export to it.
            /// If the export is for a vanilla troop (delta-only payload), we first clone the vanilla troop into the stub.
            /// Disposing the lease restores the stub to its previous state and releases it.
            /// </summary>
            public ModelLease LeaseModelCharacter()
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
                        return null;

                    if (
                        !TryExtractModelCharacterPayload(
                            FilePath,
                            Kind,
                            out var payload,
                            out var modelStringId
                        )
                    )
                        return null;

                    var stub = WCharacter.GetFreeStub();
                    if (stub == null)
                    {
                        Log.Warn("No free stub available for library model character.");
                        return null;
                    }

                    // Snapshot before applying so we can restore.
                    var snapshot = stub.Serialize();

                    // If the model refers to a vanilla troop, clone it into the stub first.
                    // This matters because exports can be delta-only (only dirty attributes).
                    if (!string.IsNullOrWhiteSpace(modelStringId))
                    {
                        var src = WCharacter.Get(modelStringId);

                        // Only do this for vanilla troops: exports may be delta-only so we need vanilla defaults first.
                        if (src != null && src.IsVanilla)
                            src.Clone(skills: true, equipments: true, intoStub: stub);
                    }

                    // Apply the export payload onto the stub (delta overlay).
                    stub.Deserialize(payload);

                    // Keep it hidden from encyclopedia just in case.
                    stub.HiddenInEncyclopedia = true;

                    return new ModelLease(stub, snapshot);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "MLibrary.Item.LeaseModelCharacter failed.");
                    return null;
                }
            }

            private static bool TryExtractModelCharacterPayload(
                string path,
                MLibraryKind kind,
                out string payload,
                out string modelStringId
            )
            {
                payload = null;
                modelStringId = null;

                try
                {
                    var doc = XDocument.Load(path, LoadOptions.None);
                    var root = doc.Root;
                    if (root == null)
                        return false;

                    // Only correct root format.
                    if (root.Name.LocalName == "Retinues")
                    {
                        var elements = root.Elements().ToList();
                        if (elements.Count == 0)
                            return false;

                        XElement modelEl = null;

                        if (kind == MLibraryKind.Character)
                        {
                            modelEl = elements.FirstOrDefault(IsCharacterElement);
                        }
                        else if (kind == MLibraryKind.Faction)
                        {
                            // First element is the faction wrapper, next are troops.
                            modelEl =
                                elements.Skip(1).FirstOrDefault(IsCharacterElement)
                                ?? elements.FirstOrDefault(IsCharacterElement);
                        }
                        else
                        {
                            modelEl = elements.FirstOrDefault(IsCharacterElement);
                        }

                        if (modelEl == null)
                            return false;

                        modelStringId = TryGetModelStringId(modelEl);
                        payload = ExtractPayload(modelEl);

                        return !string.IsNullOrWhiteSpace(payload);
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "MLibrary.Item.TryExtractModelCharacterPayload failed.");
                    return false;
                }
            }

            private static bool IsCharacterElement(XElement el)
            {
                if (el == null)
                    return false;

                // Prefer the explicit type attribute when present.
                var type = (string)el.Attribute("type");
                if (!string.IsNullOrWhiteSpace(type) && type.Contains(".Characters.WCharacter"))
                    return true;

                // Fallback on element name.
                return el.Name.LocalName.Contains("WCharacter");
            }

            private static string TryGetModelStringId(XElement el)
            {
                if (el == null)
                    return null;

                var id = (string)el.Attribute("stringId") ?? (string)el.Attribute("id");

                if (!string.IsNullOrWhiteSpace(id))
                    return id;

                var uid = (string)el.Attribute("uid");
                return TryGetStringIdFromUid(uid);
            }

            private static string TryGetStringIdFromUid(string uid)
            {
                if (string.IsNullOrWhiteSpace(uid))
                    return null;

                var sep = uid.IndexOf(':');
                if (sep <= 0 || sep >= uid.Length - 1)
                    return null;

                return uid.Substring(sep + 1);
            }

            private static string ExtractPayload(XElement el)
            {
                if (el == null)
                    return string.Empty;

                // Pretty format: the element itself is the payload, minus uid.
                var copy = new XElement(el);
                copy.SetAttributeValue("uid", null);
                return copy.ToString(SaveOptions.DisableFormatting);
            }

            public sealed class ModelLease : IDisposable
            {
                private readonly string _snapshot;

                public WCharacter Character { get; }

                internal ModelLease(WCharacter character, string snapshot)
                {
                    Character = character;
                    _snapshot = snapshot;
                }

                public void Dispose()
                {
                    try
                    {
                        if (Character == null)
                            return;

                        // Restore previous state.
                        if (!string.IsNullOrWhiteSpace(_snapshot))
                            Character.Deserialize(_snapshot);

                        // Release stub back to pool.
                        Character.IsActiveStub = false;

                        // Clear any dirty flags so it doesn't persist unwanted changes.
                        Character.MarkAllAttributesClean();
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex, "MLibrary.Item.ModelLease.Dispose failed.");
                    }
                }
            }
        }
    }
}
