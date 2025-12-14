using System;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using Retinues.Editor.Screen;
using Retinues.Helpers;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Retinues.GUI.Prefabs.Encyclopedia
{
    [ViewModelMixin]
    public sealed class UnitPageScreen : BaseViewModelMixin<EncyclopediaUnitPageVM>
    {
        public UnitPageScreen(EncyclopediaUnitPageVM vm)
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
                if (ViewModel.Obj is not CharacterObject co)
                    return; // Only character objects are supported here

                // Wrap the character object
                var character = WCharacter.Get(co);

                // Launch the editor with the character
                EditorLauncher.Launch(character);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
