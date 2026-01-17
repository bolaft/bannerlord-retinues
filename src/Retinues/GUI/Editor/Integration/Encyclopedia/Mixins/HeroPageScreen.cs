using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Configuration;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Integration.Encyclopedia.Mixins
{
    [ViewModelMixin]
    public sealed class HeroPageMixin(EncyclopediaHeroPageVM vm)
        : BasePageMixin<EncyclopediaHeroPageVM>(vm)
    {
        [DataSourceProperty]
        public override bool IsEnabled
        {
            get
            {
                if (!Settings.EnableUniversalEditor)
                    return false;

                if (ViewModel.Obj is not Hero hero)
                    return false;

                if (hero.IsDead)
                    return false;

                if (hero.Clan == null)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Opens the editor for the current hero page.
        /// </summary>
        [DataSourceMethod]
        public override void ExecuteOpenEditor()
        {
            try
            {
                if (ViewModel.Obj is not Hero hero)
                    return;

                var wh = WHero.Get(hero);
                if (wh == null)
                    return;

                EditorLauncher.Launch(EditorLaunchArgs.Universal(hero: wh));
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
