using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace CustomClanTroops.UI
{
    // Root: insert our container panel into ClanScreen
    [PrefabExtension(
        "ClanScreen",
        "descendant::Widget[./Children/ClanMembers and ./Children/ClanParties and ./Children/ClanFiefs and ./Children/ClanIncome]/Children")]
    internal class ClanScreen_TroopsPanel : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string FileName => "ClanScreen_TroopsPanel";
        public override InsertType Type => InsertType.Child;
        public override int Index => 4; // after vanilla 0..3
    }
}
