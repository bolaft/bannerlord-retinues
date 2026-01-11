using Retinues.Editor.Controllers.Doctrines;
using Retinues.UI.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Doctrines
{
    /// <summary>
    /// Doctrine feat card shown in the doctrines panel.
    /// </summary>
    public sealed class FeatVM(DoctrinesController.FeatInfo feat) : ViewModel
    {
        private readonly DoctrinesController.FeatInfo _feat = feat;

        [DataSourceProperty]
        public string Description => _feat?.Description ?? string.Empty;

        [DataSourceProperty]
        public string ProgressText =>
            _feat == null
                ? string.Empty
                : DoctrinesController.GetProgressText(_feat.Progress, _feat.Target);

        [DataSourceProperty]
        public int ProgressPercent =>
            _feat == null
                ? 0
                : DoctrinesController.GetProgressPercent(_feat.Progress, _feat.Target);

        [DataSourceProperty]
        public bool IsCompleted => _feat != null && _feat.IsCompleted;

        [DataSourceProperty]
        public bool IsRequired => _feat != null && _feat.IsRequired;

        [DataSourceProperty]
        public string RequiredText =>
            IsRequired
                ? L.S("doctrine_feat_required", "Required")
                : L.S("doctrine_feat_optional", "Optional");
    }
}
