using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.Helpers;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.GUI.Prefabs.Encyclopedia
{
    [ViewModelMixin]
    public abstract class BaseEncyclopediaPageScreen<TViewModel> : BaseViewModelMixin<TViewModel>
        where TViewModel : ViewModel
    {
        public BaseEncyclopediaPageScreen(TViewModel vm)
            : base(vm)
        {
            try
            {
                Sprites.Load("ui_clan");
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        [DataSourceProperty]
        public virtual bool IsEnabled => true;

        [DataSourceProperty]
        public virtual int MarginTop => 10;

        [DataSourceProperty]
        public virtual int MarginRight => 10;

        [DataSourceProperty]
        public Tooltip EditorHint =>
            new(L.S("encyclopedia_editor_button_hint", "Open in the editor."));

        [DataSourceMethod]
        public abstract void ExecuteOpenEditor();
    }
}
