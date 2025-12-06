using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Utilities;
using Retinues.Wrappers.Characters;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List.Rows
{
    public sealed class CharacterRowVM(ListHeaderVM header, WCharacter character)
        : ListRowVM(header, character.StringId, character.Name)
    {
        private readonly WCharacter _character = character;

        public WCharacter Character => _character;

        [DataSourceProperty]
        public override bool IsCharacter => true;

        public override void RefreshValues()
        {
            // Name may have changed, etc.
            OnPropertyChanged(nameof(Label));
        }

        [DataSourceMethod]
        public override void ExecuteSelect()
        {
            base.ExecuteSelect();
            Log.Info($"Selecting character '{Character.StringId}'");
        }
    }
}
