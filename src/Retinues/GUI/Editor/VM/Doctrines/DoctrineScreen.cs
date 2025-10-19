using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.VM.Doctrines
{
    [SafeClass]
    public sealed class DoctrineScreenVM() : BaseVM
    {
        protected override Dictionary<UIEvent, string[]> EventMap => [];

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
