using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Retinues.Utils;

[PrefabExtension(
    "ClanScreen",
    "descendant::Widget[./Children/ClanMembers and ./Children/ClanParties and ./Children/ClanFiefs and ./Children/ClanIncome]/Children"
)]
internal class ClanScreen_TroopsPanel : PrefabExtensionInsertPatch
{
    [PrefabExtensionFileName]
    public string FileName => BannerlordVersion.IsAtLeast14()
        ? "ClanScreen_TroopsPanel_BL14"
        : "ClanScreen_TroopsPanel";

    public override InsertType Type => InsertType.Child;

    public override int Index => 4;
}
