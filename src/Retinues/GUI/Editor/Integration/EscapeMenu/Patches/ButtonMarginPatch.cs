using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Retinues.Configuration;

namespace Retinues.GUI.Integration.EscapeMenu.Patches
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
            get
            {
                // If Universal Editor is disabled, don't touch margins at all.
                if (Settings.EnableUniversalEditor != true)
                    return [];

                return [new Attribute("MarginBottom", "22")];
            }
        }
    }
}
