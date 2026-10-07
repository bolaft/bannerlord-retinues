using System;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs;
using HarmonyLib;
using Retinues.Mods.Shokuho;

namespace Retinues.Tests.Cases
{
    public static class ClanScreenCompatibilityTests
    {
        [GameTest("ClanFinancePanelPreservesLegacyVisibilityBinding", "compatibility", RequiresCampaign = false)]
        public static void ClanFinancePanelPreservesLegacyVisibilityBinding()
        {
            var document = new XmlDocument();
            document.LoadXml("<Prefab><Window><Widget Id='TopPanel' IsVisible='true'/><Widget Id='FinancePanelWidget' SuggestedWidth='200'/></Window></Prefab>");
            var patch = new ClanScreen_FinancePanel_Visible();
            patch.Apply(document);

            var panel = (XmlElement)document.SelectSingleNode("//Widget[@Id='FinancePanelWidget']");
            Tests.AssertEqual("@IsFinancePanelVisible", panel.GetAttribute("IsVisible"));
            Tests.AssertEqual("200", panel.GetAttribute("SuggestedWidth"));
            Tests.AssertEqual("true", ((XmlElement)document.SelectSingleNode("//Widget[@Id='TopPanel']")).GetAttribute("IsVisible"));
            var once = document.OuterXml;
            patch.Apply(document);
            Tests.AssertEqual(once, document.OuterXml);
        }

        [GameTest("ClanFinancePanelSupportsLayoutsWithoutThePanel", "compatibility", RequiresCampaign = false)]
        public static void ClanFinancePanelSupportsLayoutsWithoutThePanel()
        {
            var document = new XmlDocument();
            document.LoadXml("<Prefab><Window><ClanScreenWidget><Children><Widget Id='TopPanel'/><ClanIncome/></Children></ClanScreenWidget></Window></Prefab>");
            var before = document.OuterXml;
            new ClanScreen_FinancePanel_Visible().Apply(document);
            Tests.AssertEqual(before, document.OuterXml);

            // A node-based extension would fail in UIExtenderEx before Apply runs.
            var type = typeof(ClanScreen_FinancePanel_Visible);
            Tests.AssertTrue(typeof(CustomPatch<XmlDocument>).IsAssignableFrom(type));
            var registration = (PrefabExtensionAttribute)Attribute.GetCustomAttribute(type, typeof(PrefabExtensionAttribute));
            Tests.AssertEqual("ClanScreen", registration.Movie);
            Tests.AssertTrue(string.IsNullOrEmpty(registration.XPath));
        }

        [GameTest("ShokuhoHelperIsIgnoredByAutomaticHarmonyDiscovery", "compatibility", RequiresCampaign = false)]
        public static void ShokuhoHelperIsIgnoredByAutomaticHarmonyDiscovery()
        {
            const string owner = "Retinues.Tests.ShokuhoDiscovery";
            var harmony = new Harmony(owner);
            try
            {
                var patches = harmony.CreateClassProcessor(typeof(ShokuhoEquipmentPatcher)).Patch();
                Tests.AssertTrue(patches == null || patches.Count == 0);
            }
            finally
            {
                harmony.UnpatchAll(owner);
            }
        }
    }
}
