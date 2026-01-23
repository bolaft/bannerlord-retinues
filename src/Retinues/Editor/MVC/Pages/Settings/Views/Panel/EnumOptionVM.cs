using System;
using System.Collections.Generic;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using Retinues.Settings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Editor.MVC.Pages.Settings.Views.Panel
{
    /// <summary>
    /// Multi-choice option for enum settings (dropdown-like).
    /// Backed by IOption.ChoiceEntries.
    /// </summary>
    public sealed class EnumOptionVM : OptionVM
    {
        private readonly List<object> _values = [];
        private readonly List<string> _hints = [];

        public override bool IsEnumOption => true;

        /// <summary>
        /// Creates a ViewModel for an enum option using its declared choice entries.
        /// </summary>
        public EnumOptionVM(IOption option)
            : base(option)
        {
            Choices = [];

            var entries = option?.ChoiceEntries;
            if (entries != null)
                for (int i = 0; i < entries.Count; i++)
                {
                    var (value, label, hint) = entries[i];
                    _values.Add(value);
                    Choices.Add(label ?? value?.ToString() ?? string.Empty);
                    _hints.Add(hint ?? string.Empty);
                }

            SelectedIndex = FindIndexForCurrentValue();
        }

        [DataSourceProperty]
        public MBBindingList<string> Choices { get; private set; }

        private int _selectedIndex;

        [DataSourceProperty]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value)
                    return;

                _selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
                OnPropertyChanged(nameof(SelectedLabel));
                OnPropertyChanged(nameof(Tooltip));

                if (Option == null || IsDisabled)
                    return;

                if (value < 0 || value >= _values.Count)
                    return;

                Option.SetObject(_values[value]);
            }
        }

        [DataSourceProperty]
        public override Tooltip Tooltip
        {
            get
            {
                var hint = GetSelectedHint();
                if (string.IsNullOrWhiteSpace(hint))
                    return null;

                // Only show the hint text (no title).
                return new Tooltip(hint);
            }
        }

        [DataSourceProperty]
        public override string SelectedLabel
        {
            get
            {
                if (Choices == null)
                    return string.Empty;

                if (SelectedIndex < 0 || SelectedIndex >= Choices.Count)
                    return string.Empty;

                return Choices[SelectedIndex];
            }
        }

        /// <summary>
        /// Selects the previous enum choice.
        /// </summary>
        public override void ExecuteSelectPrevious()
        {
            if (_values.Count == 0)
                return;

            int next = SelectedIndex - 1;
            if (next < 0)
                next = _values.Count - 1;

            SelectedIndex = next;
        }

        /// <summary>
        /// Selects the next enum choice.
        /// </summary>
        public override void ExecuteSelectNext()
        {
            if (_values.Count == 0)
                return;

            int next = SelectedIndex + 1;
            if (next >= _values.Count)
                next = 0;

            SelectedIndex = next;
        }

        /// <summary>
        /// Opens a selection popup listing all enum choices and their hints.
        /// </summary>
        public override void ExecuteOpenChoicePopup()
        {
            if (Option == null || IsDisabled)
                return;

            if (_values.Count == 0)
                return;

            var elements = new List<InquiryElement>(_values.Count);

            for (int i = 0; i < _values.Count; i++)
            {
                var label =
                    (Choices != null && i >= 0 && i < Choices.Count)
                        ? (Choices[i] ?? string.Empty)
                        : string.Empty;

                var hint =
                    (i >= 0 && i < _hints.Count) ? (_hints[i] ?? string.Empty) : string.Empty;

                elements.Add(new InquiryElement(_values[i], label, null, true, hint));
            }

            var title = new TextObject("{=!}" + (Option.Name ?? string.Empty));
            var description = string.IsNullOrWhiteSpace(Option.Description)
                ? null
                : new TextObject("{=!}" + Option.Description);

            // Use the multi-selection inquiry UI so each entry can show its hint on hover.
            // Enum options remain single-choice (maxSelectable=1).
            Inquiries.MultiSelectPopup(
                title: title,
                elements: elements,
                onSelect: selected =>
                {
                    if (selected == null || selected.Count == 0)
                        return;

                    var chosen = selected[0]?.Identifier;
                    if (chosen == null)
                        return;

                    for (int i = 0; i < _values.Count; i++)
                    {
                        if (Equals(_values[i], chosen))
                        {
                            SelectedIndex = i;
                            return;
                        }
                    }

                    // Fallback: attempt to set directly.
                    Option.SetObject(chosen);
                },
                minSelectable: 1,
                maxSelectable: 1,
                description: description
            );
        }

        /// <summary>
        /// Updates selection bindings when the option changes externally.
        /// </summary>
        protected override void OnOptionValueChanged(object newValue)
        {
            SelectedIndex = FindIndexForCurrentValue();
            OnPropertyChanged(nameof(SelectedLabel));
            OnPropertyChanged(nameof(Tooltip));
        }

        /// <summary>
        /// Gets the hint text for the currently selected choice.
        /// </summary>
        private string GetSelectedHint()
        {
            if (SelectedIndex < 0 || SelectedIndex >= _hints.Count)
                return string.Empty;

            return _hints[SelectedIndex] ?? string.Empty;
        }

        /// <summary>
        /// Resolves the current option value to a choice index.
        /// </summary>
        private int FindIndexForCurrentValue()
        {
            if (Option == null)
                return 0;

            try
            {
                var current = Option.GetObject();

                for (int i = 0; i < _values.Count; i++)
                {
                    if (Equals(current, _values[i]))
                        return i;
                }
            }
            catch
            {
                // ignore
            }

            return 0;
        }
    }
}
