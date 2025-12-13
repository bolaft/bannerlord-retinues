using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.GUI.Prefabs.ClanScreen.Patches
{
    [PrefabExtension(
        "ClanScreen",
        "descendant::Widget[./Children/ClanMembers and ./Children/ClanParties and ./Children/ClanFiefs and ./Children/ClanIncome]/Children"
    )]
    internal class ClanScreen_EditorPanel : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "ClanScreen_EditorPanel";

        public override InsertType Type => InsertType.Child;

        public override int Index => 4;
    }
}
