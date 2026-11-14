using Retinues.Diagnostics.BugReports;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Interface.Services;

namespace Retinues.Editor.MVC.Pages.Settings.Controllers
{
    public sealed class BugReportController : BaseController
    {
        public static ControllerAction<object> CreateReport { get; } =
            Action<object>("CreateBugReport")
                .DefaultTooltip(L.T("bug_report_tooltip", "Collect logs, a save copy and a screenshot into a ZIP to share with a bug report."))
                .ExecuteWith(_ => BugReportService.ShowDialog());
    }
}
