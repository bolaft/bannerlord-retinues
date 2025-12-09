using System;
using Retinues.Engine;
using Retinues.Model.Characters;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List.Character
{
    /// <summary>
    /// Row representing a troop character in the list.
    /// </summary>
    public sealed class CharacterListRowVM(
        ListHeaderVM header,
        WCharacter character,
        bool civilian = false
    ) : ListRowVM(header, character?.StringId ?? string.Empty)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal readonly WCharacter Character = character;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Type Flags                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsCharacter => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Name & Indentation                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        [EventListener(UIEvent.Name)]
        public string Name => Character.Name;

        [DataSourceProperty]
        public string Indentation
        {
            get
            {
                if (Character == null || Character.IsRoot)
                {
                    return string.Empty;
                }

                int n = Math.Max(0, Character.Depth);
                return new string(' ', n * 4);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Tier                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly int _tier = character?.Tier ?? 0;

        [DataSourceProperty]
        public StringItemWithHintVM TierIconData =>
            CampaignUIHelper.GetCharacterTierData(Character.Base, isBig: true);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string FormationClassIcon => Icons.GetFormationClassIcon(Character);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Civilian                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private readonly bool _isCivilian = civilian;

        [DataSourceProperty]
        public bool IsCivilian => _isCivilian;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        [EventListener(UIEvent.Appearance)]
        public object Image => Character?.GetImage(_isCivilian);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal override IComparable GetSortValue(ListSortKey sortKey)
        {
            return sortKey switch
            {
                ListSortKey.Name => Name,
                ListSortKey.Tier => _tier,
                _ => Name,
            };
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

            var tierText = Character.Tier.ToString();
            if (!string.IsNullOrEmpty(tierText) && tierText.IndexOf(filter, comparison) >= 0)
            {
                return true;
            }

            var cultureName = Character.Culture?.Name;
            if (!string.IsNullOrEmpty(cultureName) && cultureName.IndexOf(filter, comparison) >= 0)
            {
                return true;
            }

            return false;
        }
    }
}
