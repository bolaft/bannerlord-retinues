using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Utilities;
using Retinues.Wrappers.Characters;
using TaleWorlds.Library;

namespace Retinues.Editor.VM
{
    public sealed class CharacterListElementVM(ListHeaderVM header, WCharacter character)
        : ListElementVM(header, character.StringId, character.Name)
    {
        private readonly WCharacter _character = character;

        public WCharacter Character => _character;

        [DataSourceProperty]
        public string TierText => $"T{_character.Level}";

        [DataSourceProperty]
        public override bool IsCharacter => true;

        public override void RefreshValues()
        {
            // Name may have changed, etc.
            OnPropertyChanged(nameof(Label));
            OnPropertyChanged(nameof(TierText));
        }

        [DataSourceMethod]
        public override void ExecuteSelect()
        {
            base.ExecuteSelect();
            Log.Info($"Selecting character '{Character.StringId}'");
        }
    }
}
