using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.GUI.Prefabs.ClanScreen.Patches
{
    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Constant_CollapserWidth : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "ClanScreen_Constant_CollapserWidth";

        public override InsertType Type => InsertType.Child;

        public override int Index => 999;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Constant_CollapserHeight : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "ClanScreen_Constant_CollapserHeight";

        public override InsertType Type => InsertType.Child;

        public override int Index => 1000;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Constant_Sort1Width : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "ClanScreen_Constant_Sort1Width";

        public override InsertType Type => InsertType.Child;

        public override int Index => 1001;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Constant_Sort1Height : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "ClanScreen_Constant_Sort1Height";

        public override InsertType Type => InsertType.Child;

        public override int Index => 1002;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Constant_ScrollHeaderWidth : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "ClanScreen_Constant_ScrollHeaderWidth";

        public override InsertType Type => InsertType.Child;

        public override int Index => 1003;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Constant_ScrollHeaderHeight : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "ClanScreen_Constant_ScrollHeaderHeight";

        public override InsertType Type => InsertType.Child;

        public override int Index => 1004;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Constant_ExpandIndicatorWidth : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "ClanScreen_Constant_ExpandIndicatorWidth";

        public override InsertType Type => InsertType.Child;

        public override int Index => 1005;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Constant_ExpandIndicatorHeight : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName]
        public string FileName => "ClanScreen_Constant_ExpandIndicatorHeight";

        public override InsertType Type => InsertType.Child;

        public override int Index => 1006;
    }
}
