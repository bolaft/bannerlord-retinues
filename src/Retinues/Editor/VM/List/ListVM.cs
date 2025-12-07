using System;
using System.Linq;
using Retinues.Editor.VM.List.Rows;
using Retinues.Utilities;
using Retinues.Wrappers.Characters;
using Retinues.Wrappers.Factions;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.List
{
    /// <summary>
    /// Main list ViewModel; driven by shared faction and character state.
    /// </summary>
    public class ListVM : BaseStatefulVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Fields                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private const int SortButtonsTotalWidth = 586;

        private MBBindingList<ListHeaderVM> _headers = [];
        private ListRowVM _selectedElement;

        private MBBindingList<ListSortButtonVM> _sortButtons = [];
        private string _filterText = string.Empty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Construction                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public ListVM()
        {
            FactionChanged += OnStateFactionChanged;
            CharacterChanged += OnStateCharacterChanged;
        }

        public override void OnFinalize()
        {
            FactionChanged -= OnStateFactionChanged;
            CharacterChanged -= OnStateCharacterChanged;
            base.OnFinalize();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Headers / Selection                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<ListHeaderVM> Headers
        {
            get => _headers;
            private set
            {
                if (ReferenceEquals(value, _headers))
                {
                    return;
                }

                _headers = value;
                OnPropertyChanged(nameof(Headers));
            }
        }

        [DataSourceProperty]
        public ListRowVM SelectedElement
        {
            get => _selectedElement;
            private set
            {
                if (ReferenceEquals(value, _selectedElement))
                {
                    return;
                }

                _selectedElement = value;
                OnPropertyChanged(nameof(SelectedElement));
            }
        }

        public ListHeaderVM AddHeader(string id, string name)
        {
            var header = new ListHeaderVM(this, id, name);

            // Insert at index 0 because the list is displayed in reverse.
            _headers.Insert(0, header);

            return header;
        }

        public void Clear()
        {
            foreach (var header in _headers)
            {
                header.Elements.Clear();
            }

            _headers.Clear();
            SelectedElement = null;
        }

        internal void OnElementSelected(ListRowVM element)
        {
            foreach (var header in _headers)
            {
                header.ClearSelectionExcept(element);
            }

            SelectedElement = element;

            if (element is CharacterRowVM characterRow)
            {
                StateCharacter = characterRow.Character;
            }
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            foreach (var header in _headers)
            {
                header.RefreshValues();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     State / Rebuild                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Forces a rebuild based on the current shared faction state.
        /// </summary>
        public void Rebuild()
        {
            RebuildFromFaction(StateFaction);
        }

        private void OnStateFactionChanged(IBaseFaction faction)
        {
            RebuildFromFaction(faction);
        }

        private void RebuildFromFaction(IBaseFaction faction)
        {
            Clear();

            if (faction == null)
            {
                return;
            }

            WCharacter firstCharacter = null;

            static void AddSection(
                ListVM list,
                ref WCharacter first,
                string headerId,
                string headerLocKey,
                string headerFallback,
                System.Collections.Generic.IEnumerable<WCharacter> troops,
                bool civilian = false
            )
            {
                var header = list.AddHeader(headerId, L.S(headerLocKey, headerFallback));

                if (troops == null)
                {
                    return;
                }

                foreach (var troop in troops)
                {
                    if (troop == null)
                    {
                        continue;
                    }

                    header.AddCharacterRow(troop, civilian);
                    first ??= troop;
                }
            }

            // Retinues.
            AddSection(
                this,
                ref firstCharacter,
                "retinues",
                "list_header_retinues",
                L.S("list_header_retinues", "Retinues"),
                faction.RosterRetinues
            );

            // Elite tree.
            AddSection(
                this,
                ref firstCharacter,
                "elite",
                "list_header_elite",
                L.S("list_header_elite", "Elite"),
                faction.RootElite?.Tree
            );

            // Regular tree.
            AddSection(
                this,
                ref firstCharacter,
                "regular",
                "list_header_regular",
                L.S("list_header_regular", "Regular"),
                faction.RootBasic?.Tree
            );

            // Militia.
            AddSection(
                this,
                ref firstCharacter,
                "militia",
                "list_header_militia",
                L.S("list_header_militia", "Militia"),
                faction.RosterMilitia
            );

            // Caravan.
            AddSection(
                this,
                ref firstCharacter,
                "caravan",
                "list_header_caravan",
                L.S("list_header_caravan", "Caravan"),
                faction.RosterCaravan
            );

            // Villagers.
            AddSection(
                this,
                ref firstCharacter,
                "villagers",
                "list_header_villagers",
                L.S("list_header_villagers", "Villagers"),
                faction.RosterVillager
            );

            // Bandits.
            AddSection(
                this,
                ref firstCharacter,
                "bandits",
                "list_header_bandits",
                L.S("list_header_bandits", "Bandits"),
                faction.RosterBandit
            );

            // Civilians.
            AddSection(
                this,
                ref firstCharacter,
                "civilians",
                "list_header_civilians",
                L.S("list_header_civilians", "Civilians"),
                faction.RosterCivilian,
                civilian: true
            );

            if (StateCharacter == null && firstCharacter != null)
            {
                StateCharacter = firstCharacter;
            }
            else if (StateCharacter != null)
            {
                UpdateSelectionFromCharacter(StateCharacter);
            }

            RefreshValues();
        }

        private void OnStateCharacterChanged(WCharacter character)
        {
            UpdateSelectionFromCharacter(character);
        }

        private void UpdateSelectionFromCharacter(WCharacter character)
        {
            if (_headers.Count == 0)
            {
                return;
            }

            if (character == null)
            {
                foreach (var header in _headers)
                {
                    header.ClearSelectionExcept(null);
                }

                SelectedElement = null;
                return;
            }

            ListRowVM match = null;

            foreach (var header in _headers)
            {
                foreach (var element in header.Elements)
                {
                    if (element is CharacterRowVM row && ReferenceEquals(row.Character, character))
                    {
                        match = element;
                        break;
                    }
                }

                if (match != null)
                {
                    break;
                }
            }

            if (match != null)
            {
                OnElementSelected(match);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Sort Buttons                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public MBBindingList<ListSortButtonVM> SortButtons
        {
            get => _sortButtons;
            private set
            {
                if (ReferenceEquals(value, _sortButtons))
                {
                    return;
                }

                _sortButtons = value;
                OnPropertyChanged(nameof(SortButtons));

                SetDynamicButtonProperties();
            }
        }

        public ListSortButtonVM AddSortButton(string id, string text, int relativeWidth)
        {
            var button = new ListSortButtonVM(this, id, text, relativeWidth);
            _sortButtons.Add(button);

            SetDynamicButtonProperties();

            return button;
        }

        internal void OnSortButtonClicked(ListSortButtonVM clicked)
        {
            if (clicked == null)
            {
                return;
            }

            foreach (var button in _sortButtons)
            {
                if (ReferenceEquals(button, clicked))
                {
                    button.CycleSortState();
                }
                else
                {
                    button.ResetSortState();
                }
            }

            OnSortChanged();
        }

        private void OnSortChanged()
        {
            // Sorting is not implemented yet.
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Filter                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        [DataSourceProperty]
        public string FilterLabel => L.S("filter_label", "Filter:");

        [DataSourceProperty]
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (string.Equals(value, _filterText, StringComparison.Ordinal))
                {
                    return;
                }

                _filterText = value ?? string.Empty;
                OnPropertyChanged(nameof(FilterText));

                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            // Filtering is not implemented yet.
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Helpers                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void SetDynamicButtonProperties()
        {
            if (_sortButtons.Count == 0)
            {
                return;
            }

            var totalRequested = _sortButtons.Sum(b => Math.Max(1, b.RequestedWidth));
            if (totalRequested <= 0)
            {
                totalRequested = _sortButtons.Count;
            }

            var remaining = SortButtonsTotalWidth;

            for (var i = 0; i < _sortButtons.Count; i++)
            {
                var button = _sortButtons[i];
                var request = Math.Max(1, button.RequestedWidth);

                int width;
                if (i == _sortButtons.Count - 1)
                {
                    width = Math.Max(1, remaining);
                }
                else
                {
                    var fraction = (double)request / totalRequested;
                    width = (int)Math.Round(SortButtonsTotalWidth * fraction);
                    if (width < 1)
                    {
                        width = 1;
                    }

                    remaining -= width;
                }

                button.SetNormalizedWidth(width);
                button.SetIsLastColumn(i == _sortButtons.Count - 1);
            }
        }
    }
}
