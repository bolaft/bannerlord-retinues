namespace Retinues.Editor.VM.List.Doctrines
{
    /// <summary>
    /// Header for a doctrine category section.
    /// </summary>
    public sealed class DoctrinesListHeader(ListVM list, string id, string name)
        : ListHeaderVM(list, id, name) { }
}
