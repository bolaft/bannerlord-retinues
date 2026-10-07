using System.Collections.Generic;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Retinues.Configuration;

[PrefabExtension("ClanScreen", "descendant::ButtonWidget[@CommandParameter.Click='0']")]
internal class ClanScreen_MembersWidth : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => [new Attribute("SuggestedWidth", "240")];
}

[PrefabExtension("ClanScreen", "descendant::ButtonWidget[@CommandParameter.Click='1']")]
internal class ClanScreen_PartiesWidth : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => [new Attribute("SuggestedWidth", "240")];
}

[PrefabExtension("ClanScreen", "descendant::ButtonWidget[@CommandParameter.Click='2']")]
internal class ClanScreen_FiefsWidth : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => [new Attribute("SuggestedWidth", "240")];
}

[PrefabExtension("ClanScreen", "descendant::ButtonWidget[@CommandParameter.Click='3']")]
internal class ClanScreen_IncomeWidth : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes => [new Attribute("SuggestedWidth", "240")];
}

[PrefabExtension("ClanScreen", "descendant::Widget[@Id='TopPanel']")]
internal class ClanScreen_TopPanel_Visible : PrefabExtensionSetAttributePatch
{
    public override List<Attribute> Attributes =>
        [new Attribute("IsVisible", "@IsTopPanelVisible")];
}

[PrefabExtension("ClanScreen", null)]
internal class ClanScreen_FinancePanel_Visible : Bannerlord.UIExtenderEx.Prefabs.CustomPatch<XmlDocument>
{
    public override string Id => nameof(ClanScreen_FinancePanel_Visible);

    public override void Apply(XmlDocument document)
    {
        // Newer game layouts omit this panel. Patch the document so UIExtenderEx
        // does not report a missing XPath target before we can check for it.
        var panel = document.SelectSingleNode("descendant::Widget[@Id='FinancePanelWidget']") as XmlElement;
        panel?.SetAttribute("IsVisible", "@IsFinancePanelVisible");
    }
}

[PrefabExtension(
    "EscapeMenu",
    "descendant::NavigatableListPanel[@Id='ButtonsContainer']/ItemTemplate/Widget"
)]
internal class EscapeMenu_ItemTemplate_Margins : PrefabExtensionSetAttributePatch
{
    // Reduce the vertical gap between ESC menu buttons (default was 30)
    public override List<Attribute> Attributes
    {
        get
        {
            // Only apply this patch when the global editor is enabled in config.
            if (!(Config.EnableGlobalEditor ?? false))
                return [];

            return [new Attribute("MarginBottom", "22")];
        }
    }
}
