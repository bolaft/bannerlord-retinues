using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.Editor.Integration.Encyclopedia.Patches
{
    /// <summary>
    /// Inserts the editor button into the encyclopedia hero page.
    /// </summary>
    [PrefabExtension(
        "EncyclopediaHeroPage",
        "descendant::Widget[./Children/ButtonWidget[@Id='BookmarkButton']]/Children"
    )]
    internal sealed class EncyclopediaHeroPage_EditorButtonPatch : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "EditorLinkButton";
        public override InsertType Type => InsertType.Child;
        public override int Index => 99;
    }
}
