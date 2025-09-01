using System;
using System.Linq;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Logic;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class EquipmentEditorVM : ViewModel
    {
        private readonly ClanManagementMixinVM _owner;

        private CharacterWrapper Troop => _owner.SelectedTroop;

        public EquipmentEditorVM(ClanManagementMixinVM owner) => _owner = owner;

        public void Refresh()
        {
            
        }
    }
}
