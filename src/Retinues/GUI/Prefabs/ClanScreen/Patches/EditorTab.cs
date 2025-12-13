using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.GUI.Prefabs.ClanScreen.Patches
{
    [PrefabExtension(
        "ClanScreen",
        "descendant::ListPanel[.//TextWidget[@Text='@MembersText'] and .//TextWidget[@Text='@PartiesText']]/Children"
    )]
    internal class ClanScreen_EditorTab : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "ClanScreen_EditorTab";

        public override InsertType Type => InsertType.Child;

        public override int Index => 2;
    }
}
