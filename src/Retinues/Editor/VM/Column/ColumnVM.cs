using System.Diagnostics.Tracing;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Utilities;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Column
{
    /// <summary>
    /// Root editor ViewModel; initializes shared state and child VMs.
    /// </summary>
    public class ColumnVM : BaseVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Controls                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly CustomizationControlsVM _customizationControls = new();

        [DataSourceProperty]
        public CustomizationControlsVM CustomizationControls => _customizationControls;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Model                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private CharacterViewModel _model;

        [DataSourceProperty]
        public CharacterViewModel Model
        {
            get => _model;
            private set
            {
                if (ReferenceEquals(value, _model))
                {
                    return;
                }

                _model = value;
                OnPropertyChanged(nameof(Model));
            }
        }

        // Rebuild the tableau model whenever the current troop changes.
        [EventListener(UIEvent.Character, UIEvent.Appearance)]
        private void RebuildModel()
        {
            var character = State.Character;
            if (character?.Base == null)
            {
                Model = null;
                return;
            }

            var co = character.Base;
            var vm = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
            vm.FillFrom(co, seed: -1);

            Model = vm;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Equipment Button                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Mode)]
        [DataSourceProperty]
        public string EquipmentButtonText =>
            EditorVM.Mode == EditorMode.Equipment
                ? L.S("close_equipment_button_text", "Back")
                : L.S("equipment_button_text", "Equipment");

        [EventListener(UIEvent.Mode)]
        [DataSourceProperty]
        public string EquipmentButtonBrush =>
            EditorVM.Mode == EditorMode.Equipment ? "ButtonBrush3" : "ButtonBrush1";

        [DataSourceMethod]
        public void ExecuteToggleEquipmentMode()
        {
            if (EditorVM.Mode == EditorMode.Equipment)
                EditorVM.SetMode(EditorMode.Character);
            else
                EditorVM.SetMode(EditorMode.Equipment);
        }
    }
}
