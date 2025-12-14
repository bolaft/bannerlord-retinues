using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.Helpers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Retinues.GUI.Prefabs.Encyclopedia
{
    [ViewModelMixin]
    public sealed class HeroPageScreen : BaseViewModelMixin<EncyclopediaHeroPageVM>
    {
        public HeroPageScreen(EncyclopediaHeroPageVM vm)
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
        public Tooltip EditorHint =>
            new(L.S("encyclopedia_editor_button_hint", "Open in the editor."));

        [DataSourceMethod]
        public void ExecuteOpenEditor()
        {
            try
            {
                if (ViewModel.Obj is not Hero hero)
                    return; // Only hero objects are supported here

                // No implementation yet
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
