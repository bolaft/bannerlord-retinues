using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Screen;
using Retinues.Model.Characters;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Retinues.GUI.Prefabs.Encyclopedia
{
    [ViewModelMixin]
    public sealed class HeroPageScreen(EncyclopediaHeroPageVM vm)
        : BaseEncyclopediaPageScreen<EncyclopediaHeroPageVM>(vm)
    {
        [DataSourceProperty]
        public override bool IsEnabled
        {
            get
            {
                if (ViewModel.Obj is not Hero hero)
                    return false; // Not a hero, cannot edit.

                if (hero.IsDead)
                    return false; // Dead heroes are not editable.

                if (hero.Clan == null)
                    return false; // No clan means no faction.

                return true;
            }
        }

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
