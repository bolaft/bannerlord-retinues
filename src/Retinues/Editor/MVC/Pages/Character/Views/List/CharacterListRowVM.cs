using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Pages.Character.Helpers;
using Retinues.Editor.MVC.Shared.Views;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Character.Views.List
{
    /// <summary>
    /// List row ViewModel for a troop character entry.
    /// Supports tree relationships through upgrade sources/targets.
    /// </summary>
    public class CharacterListRowVM(ListHeaderVM header, WCharacter character)
        : BaseListRowVM(header, character?.StringId ?? string.Empty)
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
        //                        Selection                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true when this troop is the currently selected character.
        /// </summary>
        [EventListener(UIEvent.Character, Global = true)]
        [DataSourceProperty]
        public override bool IsSelected => State.Character == Character;

        /// <summary>
        /// Selects this troop as the active character.
        /// </summary>
        [DataSourceMethod]
        public override void ExecuteSelect()
        {
            // Select this character.
            State.Character = Character;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Image                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the portrait image for this troop row.
        /// </summary>
        [DataSourceProperty]
        [EventListener(UIEvent.Appearance)]
        public object Image => Character?.GetImage(Character.IsCivilian);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Name & Indentation                   //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the localized troop name.
        /// </summary>
        [DataSourceProperty]
        [EventListener(UIEvent.Name)]
        public virtual string Name => Character.Name;

        /// <summary>
        /// Returns indentation spaces based on the troop depth in the upgrade tree.
        /// </summary>
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

        /// <summary>
        /// Gets the vanilla tier icon data for this troop.
        /// </summary>
        [DataSourceProperty]
        public StringItemWithHintVM TierIconData =>
            CampaignUIHelper.GetCharacterTierData(Character.Base, isBig: true);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Formation Class                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Gets the sprite id representing the character formation class.
        /// </summary>
        [EventListener(UIEvent.Formation)]
        [DataSourceProperty]
        public string FormationClassIcon => Icons.GetFormationClassIcon(Character);

        /// <summary>
        /// True when the formation class icon should be shown (i.e. not a civilian troop).
        /// BL12 does not support !@ negated bindings in XML, so we expose this positive
        /// variant instead of using IsVisible="!@IsCivilian" in the template.
        /// </summary>
        [DataSourceProperty]
        public bool ShowFormationClassIcon => !Character.IsCivilian;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                          Tree                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Returns true if this row participates in tree filtering/sorting.
        /// </summary>
        internal override bool IsTreeNode =>
            Character != null && !string.IsNullOrEmpty(Character.StringId);

        /// <summary>
        /// Returns the upgrade source ids used as tree parent ids.
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
        /// Returns the upgrade target ids used as tree child ids.
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
        /// Returns the row sort value for the specified sort key.
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
        /// Returns true when this row matches the provided filter text.
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
