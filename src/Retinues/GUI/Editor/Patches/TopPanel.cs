using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Retinues.Utils;

[PrefabExtension("ClanScreen", "descendant::ClanScreenWidget/Children")]
internal sealed class ClanScreen_TopPanel_Insert : PrefabExtensionInsertPatch
{
    [PrefabExtensionFileName]
    public string FileName => BannerlordVersion.IsAtLeast14()
        ? "ClanScreen_TopPanel_BL14"
        : "ClanScreen_TopPanel";

    public override InsertType Type => InsertType.Child;

    // Insert after vanilla TopPanel (which is early in the list).
    public override int Index => 3;
}
