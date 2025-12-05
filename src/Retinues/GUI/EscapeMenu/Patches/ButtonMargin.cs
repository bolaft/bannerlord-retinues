using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.GUI.EscapeMenu.Patches
{
    [PrefabExtension(
        "EscapeMenu",
        "descendant::NavigatableListPanel[@Id='ButtonsContainer']/ItemTemplate/Widget"
    )]
    internal class EscapeMenu_ItemTemplate_Margins : PrefabExtensionSetAttributePatch
    {
        // Reduce the vertical gap between ESC menu buttons (default was 30)
        public override List<Attribute> Attributes
        {
            get { return [new Attribute("MarginBottom", "22")]; }
        }
    }
}
