using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace CustomClanTroops.UI
{
    [PrefabExtension(
        "ClanScreen",
        "descendant::ListPanel[.//TextWidget[@Text='@MembersText'] and .//TextWidget[@Text='@PartiesText']]/Children")]
    internal class ClanScreen_TroopsTab : PrefabExtensionInsertPatch
    {
        // This tells UIExtenderEx which embedded XML snippet to insert (without .xml)
        [PrefabExtensionFileName] public string FileName => "ClanScreen_TroopsTab";

        // Insert as a child node of <Children> …
        public override InsertType Type => InsertType.Child;

        // …at index 2 (Members=0, Parties=1, NEW=2, Fiefs=3, …)
        public override int Index => 2;
    }
}
