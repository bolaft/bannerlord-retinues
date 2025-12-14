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
    public sealed class HeroPageScreen(EncyclopediaHeroPageVM vm)
        : BaseEncyclopediaPageScreen<EncyclopediaHeroPageVM>(vm)
    {
        [DataSourceMethod]
        public override void ExecuteOpenEditor()
        {
            try
            {
                if (ViewModel.Obj is not Hero hero)
                    return;

                // Wrap the hero object.
                var wh = WHero.Get(hero);
                if (wh == null)
                    return;

                // Launch the editor with the hero.
                EditorLauncher.Launch(wh);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
