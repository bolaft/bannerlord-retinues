using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Utilities;
using Retinues.Wrappers.Characters;
using TaleWorlds.Library;
#if BL13
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
#endif

namespace Retinues.Editor.VM.List.Rows
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                     Character Row                     //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Row representing a troop character in the list.
    /// </summary>
    public sealed class CharacterRowVM(
        ListHeaderVM header,
        WCharacter character,
        bool civilian = false
    ) : ListRowVM(header, character?.StringId ?? string.Empty)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly WCharacter _character = character;
        private readonly bool _isCivilian = civilian;

        private string _name = character?.Name ?? string.Empty;
        private int _tier = character?.Tier ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Accessors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public WCharacter Character => _character;

        [DataSourceProperty]
        public override bool IsCharacter => true;

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            private set
            {
                if (value == _name)
                {
                    return;
                }

                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        [DataSourceProperty]
        public string TierText => _tier > 0 ? $"T{_tier}" : string.Empty;

        [DataSourceProperty]
        public bool IsCivilian => _isCivilian;

        /// <summary>
        /// Simple indentation text used by the template (tree layout).
        /// </summary>
        [DataSourceProperty]
        public string Indentation
        {
            get
            {
                if (_character == null || _character.Parents.Count == 0)
                {
                    return string.Empty;
                }

                int n = Math.Max(0, _character.Depth);
                return new string(' ', n * 4);
            }
        }

        /// <summary>
        /// Image identifier used by the row template for the troop portrait.
        /// </summary>
        [DataSourceProperty]
        public object Image
        {
            get
            {
                // WCharacter.GetImage(...) returns the correct type (ImageIdentifierVM) for the current BL version.
                return _character?.GetImage(_isCivilian);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Lifecycle                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RefreshValues()
        {
            base.RefreshValues();

            Name = _character?.Name ?? string.Empty;
            _tier = _character?.Tier ?? 0;

            OnPropertyChanged(nameof(TierText));
            OnPropertyChanged(nameof(Indentation));
            OnPropertyChanged(nameof(Image));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Commands                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceMethod]
        public override void ExecuteSelect()
        {
            base.ExecuteSelect();
            Log.Info($"Selecting character '{Character?.StringId}'");
        }
    }
}
