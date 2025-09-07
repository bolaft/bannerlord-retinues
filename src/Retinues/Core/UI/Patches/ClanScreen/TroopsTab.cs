using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.Core.UI.Patches.ClanScreen
{
    [PrefabExtension(
        "ClanScreen",
        "descendant::ListPanel[.//TextWidget[@Text='@MembersText'] and .//TextWidget[@Text='@PartiesText']]/Children")]
    internal class ClanScreen_TroopsTab : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string FileName => "ClanScreen_TroopsTab";

        public override InsertType Type => InsertType.Child;

        public override int Index => 2;
    }
}
