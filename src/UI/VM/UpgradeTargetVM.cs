using System;
using TaleWorlds.Library;
using Bannerlord.UIExtenderEx.Attributes;
using CustomClanTroops.Wrappers.Objects;
using CustomClanTroops.Utils;

namespace CustomClanTroops.UI.VM
{
    public sealed class UpgradeTargetVM : ViewModel
    {
        [DataSourceProperty] public string Name => _target.Name;

        private readonly CharacterWrapper _target;

        public UpgradeTargetVM(CharacterWrapper target)
        {
            _target = target;
        }
    }
}