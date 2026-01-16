using Retinues.GUI.Editor.Shared.Views;

namespace Retinues.GUI.Editor.Modules.Pages.Doctrines.Views.List
{
    /// <summary>
    /// Header for a doctrine category section.
    /// </summary>
    public sealed class DoctrinesListHeader(BaseListVM list, string id, string name)
        : ListHeaderVM(list, id, name) { }
}
