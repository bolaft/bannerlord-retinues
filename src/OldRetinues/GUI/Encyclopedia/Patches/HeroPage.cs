using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace OldRetinues.GUI.Encyclopedia.Patches
{
    [PrefabExtension(
        "EncyclopediaHeroPage",
        "descendant::Widget[./Children/ButtonWidget[@Id='BookmarkButton']]/Children"
    )]
    internal sealed class EncyclopediaHeroPage_EditorButtonPatch : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "Encyclopedia_EditorButton";
        public override InsertType Type => InsertType.Child;
        public override int Index => 99; // end of children list
    }
}
