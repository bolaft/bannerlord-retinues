using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Tableau
{
    /// <summary>
    /// Root editor ViewModel; initializes shared state and child VMs.
    /// </summary>
    public class TableauVM : BaseVM
    {
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
        [EventListener(UIEvent.Troop, UIEvent.Appearance)]
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
    }
}
