using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Views;
using Retinues.Interface.Services;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Character.Views.List
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
        //                          Tree                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        internal override bool IsTreeNode =>
            Character != null && !string.IsNullOrEmpty(Character.StringId);

        /// <summary>
        /// Gets the parent IDs for this row in the tree.
        /// </summary>
        internal override IEnumerable<string> GetTreeParentIds()
        {
            var sources = Character?.UpgradeSources;
            if (sources == null || sources.Count() == 0)
                return null;

            var list = new List<string>();

            for (int i = 0; i < sources.Count(); i++)
            {
                var parent = sources[i];
                if (parent == null)
                    continue;

                var id = parent.StringId;
                if (!string.IsNullOrEmpty(id))
                    list.Add(id);
            }

            return list.Count > 0 ? list : null;
        }

        /// <summary>
        /// Gets the child IDs for this row in the tree.
        /// </summary>
        internal override IEnumerable<string> GetTreeChildIds()
        {
            var targets = Character?.UpgradeTargets;
            if (targets == null || targets.Count() == 0)
                return null;

            var list = new List<string>();

            for (int i = 0; i < targets.Count(); i++)
            {
                var child = targets[i];
                if (child == null)
                    continue;

                var id = child.StringId;
                if (!string.IsNullOrEmpty(id))
                    list.Add(id);
            }

            return list.Count > 0 ? list : null;
        }

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
