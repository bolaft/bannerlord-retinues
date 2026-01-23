using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using Retinues.Settings;
using Retinues.Utilities;
using TaleWorlds.Library;

namespace Retinues.Editor.Integration.Encyclopedia
{
    [ViewModelMixin]
    public abstract class BasePageMixin<TViewModel> : BaseViewModelMixin<TViewModel>
        where TViewModel : ViewModel
    {
        public BasePageMixin(TViewModel vm)
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
        public virtual EditorMode DesiredEditorMode => EditorMode.Universal;

        [DataSourceProperty]
        public virtual bool IsEnabled
        {
            get
            {
                if (
                    !Configuration.EnableUniversalEditor
                    && DesiredEditorMode == EditorMode.Universal
                )
                    return false;

                return true;
            }
        }

        [DataSourceProperty]
        public virtual int MarginTop => 10;

        [DataSourceProperty]
        public virtual int MarginRight => 10;

        [DataSourceProperty]
        public Tooltip EditorHint =>
            new(L.S("encyclopedia_editor_button_hint", "Open in the editor."));

        /// <summary>
        /// Opens the editor for the current page.
        /// </summary>
        [DataSourceMethod]
        public abstract void ExecuteOpenEditor();
    }
}
