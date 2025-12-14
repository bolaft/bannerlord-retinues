using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Screen;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

namespace Retinues.GUI.Prefabs.Encyclopedia
{
    [ViewModelMixin]
    public sealed class UnitPageScreen(EncyclopediaUnitPageVM vm)
        : BaseEncyclopediaPageScreen<EncyclopediaUnitPageVM>(vm)
    {
        [DataSourceMethod]
        public override void ExecuteOpenEditor()
        {
            try
            {
                if (ViewModel.Obj is not CharacterObject co)
                    return;

                // Wrap the character object.
                var character = WCharacter.Get(co);
                if (character == null)
                    return;

                // Launch the editor with the character.
                EditorLauncher.Launch(character);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
