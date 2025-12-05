using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.GUI.ClanScreen.Patches
{
    [PrefabExtension("ClanScreen", "descendant::Widget[@Id='FinancePanelWidget']")]
    internal class ClanScreen_FinancePanel_Visible : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes =>
            [new Attribute("IsVisible", "@ShowFinancePanel")];
    }

    [PrefabExtension("ClanScreen", "descendant::Widget[@Id='TopPanel']")]
    internal class ClanScreen_TopPanel_Visible : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => [new Attribute("IsVisible", "@ShowTopPanel")];
    }
}
