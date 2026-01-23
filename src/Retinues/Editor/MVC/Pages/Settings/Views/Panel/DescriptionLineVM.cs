using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Settings.Views.Panel
{
    public sealed class DescriptionLineVM(string text) : ViewModel
    {
        private string _text = text ?? string.Empty;

        [DataSourceProperty]
        public string Text
        {
            get => _text;
            set => SetField(ref _text, value, nameof(Text));
        }
    }
}
