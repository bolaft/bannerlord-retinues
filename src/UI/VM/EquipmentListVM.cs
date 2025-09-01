using System;
using System.Linq;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentListVM : ViewModel
    {
        private readonly ClanManagementMixinVM _owner;

        public EquipmentListVM(ClanManagementMixinVM owner) => _owner = owner;
    }
}
