using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Characters.Wrappers;
using Retinues.GUI.Editor.Events;
using Retinues.GUI.Editor.Shared.Views;
using Retinues.GUI.Services;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Modules.Pages.Character.Views.List
{
    /// <summary>
    /// Row representing a troop character in the list.
    /// </summary>
    public class CharacterListRowVM(ListHeaderVM header, WCharacter character)
        : BaseListRowVM(header, character?.StringId ?? string.Empty)
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Constructor                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal readonly WCharacter Character = character;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Type Flags                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public override bool IsCharacter => true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Character, Global = true)]
        [DataSourceProperty]
        public override bool IsSelected => State.Character == Character;

        [DataSourceMethod]
        public override void ExecuteSelect()
        {
            // Select this character.
            State.Character = Character;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Name & Indentation                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        [EventListener(UIEvent.Name)]
        public virtual string Name => Character.Name;

        [DataSourceProperty]
        public virtual string Indentation
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

        [DataSourceProperty]
        public StringItemWithHintVM TierIconData =>
            CampaignUIHelper.GetCharacterTierData(Character.Base, isBig: true);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [EventListener(UIEvent.Formation)]
        [DataSourceProperty]
        public string FormationClassIcon => Icons.GetFormationClassIcon(Character);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        [EventListener(UIEvent.Appearance)]
        public object Image => Character?.GetImage(Character.IsCivilian);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Sorting                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns the sort value for the given sort key.
        /// </summary>
        internal override IComparable GetSortValue(ListSortKey sortKey)
        {
            return sortKey switch
            {
                ListSortKey.Name => Name,
                ListSortKey.Tier => Character.Tier,
                _ => Name,
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Filtering                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if this row matches the given filter.
        /// </summary>
        internal override bool MatchesFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            var comparison = StringComparison.OrdinalIgnoreCase;

            if (!string.IsNullOrEmpty(Name) && Name.IndexOf(filter, comparison) >= 0)
                return true;

            var tierText = Character.Tier.ToString();
            if (!string.IsNullOrEmpty(tierText) && tierText.IndexOf(filter, comparison) >= 0)
                return true;

            var cultureName = Character.Culture?.Name;
            if (!string.IsNullOrEmpty(cultureName) && cultureName.IndexOf(filter, comparison) >= 0)
                return true;

            return false;
        }
    }
}
