using System;
using System.Linq;
using Retinues.Game;
using Retinues.Game.Wrappers;
using Retinues.GUI.Editor.VM;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor
{
    public abstract class BaseComponent : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Quick Access                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected static EditorVM Editor =>
            EditorVM.Instance ?? throw new Exception("EditorVM instance is null");
        protected static WFaction SelectedFaction => Editor.Faction ?? Player.Clan;
        protected static WCharacter SelectedTroop =>
            Editor.TroopScreen?.TroopList?.Selection?.Troop
            ?? SelectedFaction.Troops.FirstOrDefault();
        protected static WEquipment SelectedEquipment =>
            Editor.EquipmentScreen?.Equipment ?? SelectedTroop?.Loadout?.Battle;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Data Bindings                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _isVisible = false;

        [DataSourceProperty]
        public virtual bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value)
                    return;
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public void Show() => IsVisible = true;

        public void Hide() => IsVisible = false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected static int BatchInput(bool capped = true)
        {
            if (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
                return capped ? 5 : 1000;
            if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
                return 5;
            return 1;
        }
    }
}
