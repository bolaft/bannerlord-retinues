using System;
using Retinues.Interface.Widgets.TextEditing;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Interface.Services.Popups
{
    internal sealed class BugReportPopupVM : ViewModel
    {
        private readonly Func<string, string> _submit;
        private readonly Action _close;
        private string _description = "", _error = "";
        private bool _closed;

        internal BugReportPopupVM(Func<string, string> submit, Action close)
        { _submit = submit; _close = close; }

        [DataSourceProperty] public string TitleText => L.S("bug_report_title", "Bug Report");
        [DataSourceProperty] public string IntroductionText => L.S("bug_report_description",
            "Create a ZIP with logs, settings, your mod list, a screenshot and a copy of this campaign's last save (no unsaved progress). Review it, then submit a report in the Bugs tab on the Retinues Nexus Mods page, including the ZIP or a download link. Nothing is uploaded automatically.\n\nDescribe the problem (optional):");
        [DataSourceProperty] public string CreateText => L.S("bug_report_create", "Create ZIP");
        [DataSourceProperty] public string CancelText => TaleWorlds.Core.GameTexts.FindText("str_cancel").ToString();
        [DataSourceProperty] public string CountText => L.T("bug_report_character_count", "{COUNT} / {LIMIT} characters")
            .SetTextVariable("COUNT", _description.Length).SetTextVariable("LIMIT", MultilineTextBuffer.CharacterLimit).ToString();
        [DataSourceProperty] public string ErrorText => _error;
        [DataSourceProperty] public string Description
        {
            get => _description;
            set
            {
                value ??= "";
                if (_description == value) return;
                _description = value;
                OnPropertyChangedWithValue(value);
                OnPropertyChanged(nameof(CountText));
            }
        }

        public void ExecuteCreate()
        {
            if (_closed) return;
            try
            {
                _error = _submit(_description) ?? "";
                OnPropertyChanged(nameof(ErrorText));
                if (_error.Length == 0) ExecuteCancel();
            }
            catch (Exception e)
            {
                Log.Exception(e, "Bug report dialog submission failed.");
                _error = L.S("bug_report_failed", "Could not create the bug report. Check the Retinues log and available disk space.");
                OnPropertyChanged(nameof(ErrorText));
            }
        }

        public void ExecuteCancel()
        {
            if (_closed) return;
            _closed = true;
            _close();
        }
    }
}
