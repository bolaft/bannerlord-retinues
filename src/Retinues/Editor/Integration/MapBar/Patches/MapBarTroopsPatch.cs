#if BL12
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.Editor.Integration.MapBar.Patches
{
    /// <summary>
    /// Injects the Troops navigation button into the map bar (BL12).
    /// The BL12 MapBar uses individual named brushes and a flat ListPanel,
    /// so the button is inserted as a child of that panel at index 4 (after Party).
    /// </summary>
    [PrefabExtension("MapBar", "descendant::Widget[@Id='MapBar']/Children/ListPanel/Children")]
    internal sealed class MapBarTroopsButtonPatch : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "MapBarTroopsButton";

        public override InsertType Type => InsertType.Child;

        // 0=Escape, 1=Character, 2=Inventory, [3=Troops], 4=Party, 5=Quests, 6=Clan, 7=Kingdom
        // Mirrors BL13 which inserts before Party (index 3 without NavalDLC).
        public override int Index => 3;
    }
}
#endif
