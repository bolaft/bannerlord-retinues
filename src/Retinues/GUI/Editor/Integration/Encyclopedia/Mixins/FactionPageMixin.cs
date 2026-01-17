using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Retinues.GUI.Editor.Integration.Encyclopedia.Mixins
{
    [ViewModelMixin]
    public sealed class FactionPageMixin(EncyclopediaFactionPageVM vm)
        : BasePageMixin<EncyclopediaFactionPageVM>(vm)
    {
        [DataSourceProperty]
        public override int MarginTop => 8;

        [DataSourceProperty]
        public override EditorMode DesiredEditorMode
        {
            get
            {
                if (ViewModel.Obj is not Kingdom kingdom)
                    return EditorMode.Universal;

                var wk = WKingdom.Get(kingdom);
                if (wk == null)
                    return EditorMode.Universal;

                return Player.Kingdom == wk ? EditorMode.Player : EditorMode.Universal;
            }
        }

        /// <summary>
        /// Opens the editor for the current faction page.
        /// </summary>
        [DataSourceMethod]
        public override void ExecuteOpenEditor()
        {
            try
            {
                if (ViewModel.Obj is not Kingdom kingdom)
                    return;

                var wk = WKingdom.Get(kingdom);
                if (wk == null)
                    return;

                if (Player.Kingdom == wk)
                {
                    EditorLauncher.Launch(EditorLaunchArgs.Player(faction: wk));
                    return;
                }

                // Other kingdoms keep the old behavior: universal, culture-selected.
                var culture = wk.Culture;
                if (culture == null)
                    return;

                EditorLauncher.Launch(EditorLaunchArgs.Universal(faction: culture));
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
