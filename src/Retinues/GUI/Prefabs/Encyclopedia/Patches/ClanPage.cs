using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.GUI.Prefabs.Encyclopedia.Patches
{
    [PrefabExtension(
        "EncyclopediaClanPage",
        "descendant::ListPanel[./Children/ButtonWidget[@Id='BookmarkButton']]/Children"
    )]
    internal sealed class EncyclopediaClanPage_EditorButtonPatch : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "EditorLinkButton";

        public override InsertType Type => InsertType.Child;
        public override int Index => 3;
    }
}
