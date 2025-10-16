using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Doctrines
{
    [SafeClass]
    public sealed class DoctrineScreenVM() : BaseComponent
    {
        private MBBindingList<DoctrineColumnVM> _columns;

        [DataSourceProperty]
        public MBBindingList<DoctrineColumnVM> Columns
        {
            get
            {
                _columns ??= DoctrineColumnVM.CreateColumns();
                return _columns;
            }
        }
    }
}
