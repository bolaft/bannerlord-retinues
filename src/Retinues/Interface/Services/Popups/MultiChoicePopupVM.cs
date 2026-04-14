using System;
using System.Collections.Generic;
using Retinues.Utilities;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Retinues.Interface.Services.Popups
{
    internal sealed class MultiChoicePopupVM : ViewModel
    {
        public MultiChoicePopupVM(
            TextObject title,
            TextObject description,
            IReadOnlyList<(TextObject Label, Action Callback)> choices,
            Action close
        )
        {
            TitleText = title?.ToString() ?? string.Empty;
            DescriptionText = description?.ToString() ?? string.Empty;
            Buttons = [];

            foreach (var (label, callback) in choices)
            {
                var captured = callback;
                Buttons.Add(
                    new ChoiceButtonVM(
                        label?.ToString() ?? string.Empty,
                        () =>
                        {
                            close?.Invoke();
                            try
                            {
                                captured?.Invoke();
                            }
                            catch (Exception e)
                            {
                                Log.Exception(e, "MultiChoicePopup: choice callback threw.");
                            }
                        }
                    )
                );
            }
        }

        [DataSourceProperty]
        public string TitleText { get; }

        [DataSourceProperty]
        public string DescriptionText { get; }

        [DataSourceProperty]
        public MBBindingList<ChoiceButtonVM> Buttons { get; } = [];

        internal sealed class ChoiceButtonVM : ViewModel
        {
            private readonly Action _callback;

            public ChoiceButtonVM(string label, Action callback)
            {
                Label = label;
                _callback = callback;
            }

            [DataSourceProperty]
            public string Label { get; }

            public void ExecuteAction() => _callback?.Invoke();
        }
    }
}
