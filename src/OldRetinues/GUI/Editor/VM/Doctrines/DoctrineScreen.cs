using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Doctrines
{
    /// <summary>
    /// ViewModel hosting doctrine columns and their VMs.
    /// </summary>
    [SafeClass]
    public sealed class DoctrineScreenVM() : BaseVM
    {
        protected override Dictionary<UIEvent, string[]> EventMap => [];

        private MBBindingList<DoctrineColumnVM> _columns;

        [DataSourceProperty]
        /// <summary>
        /// Lazy-initialized list of doctrine columns.
        /// </summary>
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
