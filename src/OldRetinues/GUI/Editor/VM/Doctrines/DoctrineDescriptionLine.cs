using System.Collections.Generic;
using Retinues.Utils;
using TaleWorlds.Library;

namespace OldRetinues.GUI.Editor.VM.Doctrines
{
    [SafeClass]
    public sealed class DoctrineDescriptionLineVM(string text) : BaseVM
    {
        protected override Dictionary<UIEvent, string[]> EventMap => [];

        private string _text = text;

        [DataSourceProperty]
        public string Text => _text;
    }
}
