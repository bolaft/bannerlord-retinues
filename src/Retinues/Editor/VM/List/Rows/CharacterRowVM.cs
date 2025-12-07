using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Utilities;
using Retinues.Wrappers.Characters;
using TaleWorlds.Library;
# if BL13
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
# endif

namespace Retinues.Editor.VM.List.Rows
{
    public sealed class CharacterRowVM(ListHeaderVM header, WCharacter character)
        : ListRowVM(header, character.StringId)
    {
        private readonly WCharacter _character = character;

        public WCharacter Character => _character;

        [DataSourceProperty]
        public override bool IsCharacter => true;

        [DataSourceProperty]
        public string Indentation
        {
            get
            {
                if (_character.Parents.Count == 0)
                    return string.Empty;

                // Tier-based indentation
                int n = Math.Max(0, _character.Tier - 1);
                return new string(' ', n * 4);
            }
        }

        [DataSourceProperty]
        public string Name => _character.Name;

        /* ━━━━━━━━━ Image ━━━━━━━━ */

        [DataSourceProperty]
        public ImageIdentifierVM Image => _character.Image;

        // ━━━━━━━━━ Existing bits ━━━━━━━━━

        public override void RefreshValues()
        {
            // Name may have changed, etc.
            OnPropertyChanged(nameof(ImageIdentifier));
        }

        [DataSourceMethod]
        public override void ExecuteSelect()
        {
            base.ExecuteSelect();
            Log.Info($"Selecting character '{Character.StringId}'");
        }
    }
}
