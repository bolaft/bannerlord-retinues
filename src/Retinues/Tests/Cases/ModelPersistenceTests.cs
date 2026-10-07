using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Retinues.Framework.Model;
using Retinues.Framework.Model.Attributes;
using Retinues.Framework.Model.Persistence;

namespace Retinues.Tests.Cases
{
    // A real MBase/MAttribute consumer with no Bannerlord object registry dependencies.
    public sealed class PersistenceContractModel() : MBase<object>(new object())
    {
        private MAttribute<int> CountAttribute => Attribute(0, name: "Count");
        private MAttribute<string> NameAttribute => Attribute("", name: "Name");
        private MAttribute<float> RatioAttribute => Attribute(0f, name: "Ratio");
        private MAttribute<bool> EnabledAttribute => Attribute(false, name: "Enabled");
        private MAttribute<List<string>> QueueAttribute => Attribute(new List<string>(), name: "Queue");
        private MAttribute<Dictionary<string, int>> StocksAttribute => Attribute(new Dictionary<string, int>(), name: "Stocks");
        private MAttribute<int> SessionAttribute => Attribute(0, persistent: false, name: "Session");
        public int Count { get => CountAttribute.Get(); set => CountAttribute.Set(value); }
        public string Name { get => NameAttribute.Get(); set => NameAttribute.Set(value); }
        public float Ratio { get => RatioAttribute.Get(); set => RatioAttribute.Set(value); }
        public bool Enabled { get => EnabledAttribute.Get(); set => EnabledAttribute.Set(value); }
        public List<string> Queue { get => QueueAttribute.Get(); set => QueueAttribute.Set(value); }
        public Dictionary<string, int> Stocks { get => StocksAttribute.Get(); set => StocksAttribute.Set(value); }
        public int Session { get => SessionAttribute.Get(); set => SessionAttribute.Set(value); }
    }

    public static class ModelPersistenceTests
    {
        [GameTest("FrozenModelPayloadStillLoads", "save-contracts", RequiresCampaign = false)]
        public static void FrozenModelPayloadStillLoads()
        {
            using var stream = typeof(ModelPersistenceTests).Assembly.GetManifestResourceStream("Retinues.Tests.Fixtures.model-v1.xml");
            Tests.AssertNotNull(stream, "The frozen fixture is embedded in Debug builds.");
            using var reader = new StreamReader(stream);
            var model = new PersistenceContractModel();
            model.Deserialize(reader.ReadToEnd());
            Tests.AssertEqual(17, model.Count);
            Tests.AssertEqual("Garde & <Élite> 日本", model.Name);
            Tests.AssertEqual(1.25f, model.Ratio);
            Tests.AssertTrue(model.Enabled);
            Tests.AssertTrue(model.Queue.SequenceEqual(new[] { "0|crafted_a", "1|bow_b" }));
            Tests.AssertEqual(3, model.Stocks["crafted_a"]);
            Tests.AssertEqual(2, model.Stocks["bow_b"]);
            Tests.AssertFalse(MBase<object>.IsRestoringFromPersistence, "The restore scope does not leak.");
        }

        [GameTest("ModelRoundTripsAcrossCulturesAndRepeatedLoads", "save-contracts", RequiresCampaign = false)]
        public static void ModelRoundTripsAcrossCulturesAndRepeatedLoads(GameTestContext ctx)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                foreach (string culture in new[] { "en-US", "fr-FR", "tr-TR" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                    for (int trial = 0; trial < 40; trial++)
                        ctx.Case(trial, () =>
                        {
                            var original = new PersistenceContractModel
                            {
                                Count = ctx.Random.Next(), Name = "Élite <&> 日本 " + ctx.Random.Next(),
                                Ratio = (float)(ctx.Random.NextDouble() * 900), Enabled = trial % 2 == 0,
                                Queue = Enumerable.Range(0, trial % 9).Select(i => i + "|item_" + i).ToList(),
                                Stocks = new Dictionary<string, int> { ["item_a"] = trial, ["item_b"] = trial * 3 },
                                Session = 999
                            };
                            string payload = original.SerializeAll();
                            Tests.AssertTrue(!string.IsNullOrEmpty(payload), "Serialization cannot silently return an empty payload.");
                            Tests.AssertTrue(XDocument.Parse(payload).Root.Element("Session") == null, "Transient state is excluded.");
                            for (int generation = 0; generation < 4; generation++)
                            {
                                var restored = new PersistenceContractModel();
                                restored.Deserialize(payload);
                                Tests.AssertEqual(original.Count, restored.Count);
                                Tests.AssertEqual(original.Name, restored.Name);
                                Tests.AssertEqual(original.Ratio, restored.Ratio, "Floating-point values are culture invariant.");
                                Tests.AssertEqual(original.Enabled, restored.Enabled);
                                Tests.AssertTrue(original.Queue.SequenceEqual(restored.Queue));
                                Tests.AssertTrue(original.Stocks.OrderBy(p => p.Key).SequenceEqual(restored.Stocks.OrderBy(p => p.Key)));
                                Tests.AssertEqual(0, restored.Session);
                                payload = restored.Serialize();
                                Tests.AssertTrue(!string.IsNullOrEmpty(payload), "Loaded attributes remain eligible for the next save.");
                            }
                        });
                }
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [GameTest("UnknownAndInvalidModelVersionsDoNotOverwriteState", "save-contracts", RequiresCampaign = false)]
        public static void UnknownAndInvalidModelVersionsDoNotOverwriteState()
        {
            var model = new PersistenceContractModel { Count = 77, Name = "kept" };
            foreach (string input in new[] { "", "not XML", "<broken", "<Model v='999'><Count t='int'>5</Count></Model>" })
            {
                model.Deserialize(input);
                Tests.AssertEqual(77, model.Count);
                Tests.AssertEqual("kept", model.Name);
                Tests.AssertFalse(MBase<object>.IsRestoringFromPersistence);
            }
            model.Deserialize("<Model v='1.0'><FutureAttribute t='int'>2</FutureAttribute><Count t='int'>5</Count></Model>");
            Tests.AssertEqual(5, model.Count, "An unknown optional attribute does not hide known data.");
        }

        [GameTest("CompressedSaveChunksPreserveLargeUnicodePayloads", "save-contracts", RequiresCampaign = false)]
        public static void CompressedSaveChunksPreserveLargeUnicodePayloads(GameTestContext ctx)
        {
            var type = typeof(MPersistenceBehavior);
            var pack = type.GetMethod("PackToBase64Gzip", BindingFlags.Static | BindingFlags.NonPublic);
            var unpack = type.GetMethod("UnpackFromBase64Gzip", BindingFlags.Static | BindingFlags.NonPublic);
            var split = type.GetMethod("SplitIntoParts", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (int size in new[] { 0, 1, 23999, 24000, 24001, 100000 })
            {
                var chars = Enumerable.Range(0, size).Select(_ => (char)ctx.Random.Next(0x4E00, 0x9F00)).ToArray();
                string original = new string(chars);
                string packed = (string)pack.Invoke(null, new object[] { original });
                var parts = (List<string>)split.Invoke(null, new object[] { packed, 24000 });
                Tests.AssertTrue(parts.All(p => p.Length <= 24000), "Each save entry respects the storage limit.");
                Tests.AssertEqual(original, (string)unpack.Invoke(null, new object[] { string.Concat(parts) }));
                if (size == 100000)
                    Tests.AssertTrue(parts.Count > 1, "The fixture actually exercises multiple chunks.");
            }
        }
    }
}
