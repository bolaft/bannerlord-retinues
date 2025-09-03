using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace CustomClanTroops.UI.Patches.ClanScreen
{
    // Each patch inserts ONE constant under <Constants> in ClanScreen.xml
    // We point to an embedded XML file via [PrefabExtensionFileName].

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Const_CollapserWidth : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string FileName => "ClanScreen_Const_CollapserWidth";
        
        public override InsertType Type => InsertType.Child;
        
        public override int Index => 999;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Const_CollapserHeight : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string FileName => "ClanScreen_Const_CollapserHeight";
        
        public override InsertType Type => InsertType.Child;
        
        public override int Index => 1000;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Const_Sort1Width : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string FileName => "ClanScreen_Const_Sort1Width";
        
        public override InsertType Type => InsertType.Child;
        
        public override int Index => 1001;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Const_Sort1Height : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string FileName => "ClanScreen_Const_Sort1Height";
        
        public override InsertType Type => InsertType.Child;
        
        public override int Index => 1002;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Const_ScrollHeaderWidth : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string FileName => "ClanScreen_Const_ScrollHeaderWidth";
        
        public override InsertType Type => InsertType.Child;
        
        public override int Index => 1003;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Const_ScrollHeaderHeight : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string FileName => "ClanScreen_Const_ScrollHeaderHeight";
        
        public override InsertType Type => InsertType.Child;
        
        public override int Index => 1004;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Const_ExpandIndicatorWidth : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string FileName => "ClanScreen_Const_ExpandIndicatorWidth";
        
        public override InsertType Type => InsertType.Child;
        
        public override int Index => 1005;
    }

    [PrefabExtension("ClanScreen", "descendant::Constants")]
    internal sealed class Const_ExpandIndicatorHeight : PrefabExtensionInsertPatch
    {
        [PrefabExtensionFileName] public string FileName => "ClanScreen_Const_ExpandIndicatorHeight";
        
        public override InsertType Type => InsertType.Child;
        
        public override int Index => 1006;
    }
}
