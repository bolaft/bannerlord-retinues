using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace Retinues.GUI.Prefabs.ClanScreen.Patches
{
    [PrefabExtension("ClanScreen", "descendant::ButtonWidget[@CommandParameter.Click='0']")]
    internal class ClanScreen_MembersWidth : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => [new Attribute("SuggestedWidth", "240")];
    }

    [PrefabExtension("ClanScreen", "descendant::ButtonWidget[@CommandParameter.Click='1']")]
    internal class ClanScreen_PartiesWidth : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => [new Attribute("SuggestedWidth", "240")];
    }

    [PrefabExtension("ClanScreen", "descendant::ButtonWidget[@CommandParameter.Click='2']")]
    internal class ClanScreen_FiefsWidth : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => [new Attribute("SuggestedWidth", "240")];
    }

    [PrefabExtension("ClanScreen", "descendant::ButtonWidget[@CommandParameter.Click='3']")]
    internal class ClanScreen_IncomeWidth : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => [new Attribute("SuggestedWidth", "240")];
    }
}
