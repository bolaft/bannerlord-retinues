using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

[PrefabExtension("ClanScreen", "descendant::ClanScreenWidget/Children")]
internal sealed class ClanScreen_TopPanel_Insert : PrefabExtensionInsertPatch
{
    [PrefabExtensionFileName]
    public string FileName => "ClanScreen_TopPanel";

    public override InsertType Type => InsertType.Child;

    // Insert after vanilla TopPanel (which is early in the list).
    public override int Index => 3;
}
