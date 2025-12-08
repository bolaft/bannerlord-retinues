using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Engine;
using Retinues.Wrappers.Characters;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

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
        public WCharacter Character => _character;
        private readonly bool _isCivilian = civilian;

        private string _name = character?.Name ?? string.Empty;
        private int _tier = character?.Tier ?? 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Accessors                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public StringItemWithHintVM TierIconData =>
            CampaignUIHelper.GetCharacterTierData(_character.Base, isBig: true);

        [DataSourceProperty]
        public string FormationClassIcon => Icons.GetFormationClassIcon(_character);

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
        public bool IsCivilian => _isCivilian;

        /// <summary>
        /// Simple indentation text used by the template (tree layout).
        /// </summary>
        [DataSourceProperty]
        public string Indentation
        {
            get
            {
                if (_character == null || _character.IsRoot)
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
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal override IComparable GetSortValue(ListSortKey sortKey)
        {
            switch (sortKey)
            {
                case ListSortKey.Name:
                    return Name ?? string.Empty;

                case ListSortKey.Tier:
                    return _tier;

                default:
                    return Name ?? string.Empty;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Filtering                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal override bool MatchesFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            var comparison = StringComparison.OrdinalIgnoreCase;

            if (!string.IsNullOrEmpty(Name) && Name.IndexOf(filter, comparison) >= 0)
            {
                return true;
            }

            var tierText = _character.Tier.ToString();
            if (!string.IsNullOrEmpty(tierText) && tierText.IndexOf(filter, comparison) >= 0)
            {
                return true;
            }

            var cultureName = _character.Culture?.Name ?? string.Empty;
            if (!string.IsNullOrEmpty(cultureName) && cultureName.IndexOf(filter, comparison) >= 0)
            {
                return true;
            }

            return false;
        }
    }
}
