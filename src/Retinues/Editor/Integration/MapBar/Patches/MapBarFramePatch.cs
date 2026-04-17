#if BL12
using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.Editor.Integration.MapBar.Patches
{
    /// <summary>
    /// Widens the fixed-size left-bar frame overlay widget (BL12) to cover the injected
    /// Troops button.  The vanilla frame is 554 px wide (7 buttons); inserting one extra
    /// button (width ~78 px + 2 px margin = 80 px) before Party shifts all subsequent
    /// buttons right by 80 px, so the frame grows to 634 px.
    /// </summary>
    [PrefabExtension("MapBar", "descendant::Widget[@Sprite='MapBar\\mapbar_left_frame']")]
    internal sealed class MapBarFrameWidthPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => [new Attribute("SuggestedWidth", "634")];
    }

    // The frame widget also positions unread-notification badges at hardcoded
    // PositionXOffset values. Inserting Troops before Party shifts Party, Quests,
    // Clan, and Kingdom each by +80 px.

    [PrefabExtension(
        "MapBar",
        "descendant::MapBarUnreadBrushWidget[@UnreadTextWidget='PartyUnreadText']"
    )]
    internal sealed class MapBarPartyBadgePatch : PrefabExtensionSetAttributePatch
    {
        // 231 + 80 = 311
        public override List<Attribute> Attributes => [new Attribute("PositionXOffset", "311")];
    }

    [PrefabExtension(
        "MapBar",
        "descendant::MapBarUnreadBrushWidget[@UnreadTextWidget='QuestsUnreadText']"
    )]
    internal sealed class MapBarQuestsBadgePatch : PrefabExtensionSetAttributePatch
    {
        // 310 + 80 = 390
        public override List<Attribute> Attributes => [new Attribute("PositionXOffset", "390")];
    }

    [PrefabExtension(
        "MapBar",
        "descendant::MapBarUnreadBrushWidget[@UnreadTextWidget='ClanUnreadText']"
    )]
    internal sealed class MapBarClanBadgePatch : PrefabExtensionSetAttributePatch
    {
        // 385 + 80 = 465
        public override List<Attribute> Attributes => [new Attribute("PositionXOffset", "465")];
    }

    [PrefabExtension(
        "MapBar",
        "descendant::MapBarUnreadBrushWidget[@UnreadTextWidget='KingdomUnreadText']"
    )]
    internal sealed class MapBarKingdomBadgePatch : PrefabExtensionSetAttributePatch
    {
        // 463 + 80 = 543
        public override List<Attribute> Attributes => [new Attribute("PositionXOffset", "543")];
    }
}
#endif
