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
    public sealed class ClanPageMixin(EncyclopediaClanPageVM vm)
        : BasePageMixin<EncyclopediaClanPageVM>(vm)
    {
        [DataSourceProperty]
        public override int MarginTop => 6;

        [DataSourceProperty]
        public override int MarginRight => 0;

        [DataSourceProperty]
        public override EditorMode DesiredEditorMode
        {
            get
            {
                if (ViewModel.Obj is not Clan clan)
                    return EditorMode.Universal;

                var wc = WClan.Get(clan);
                if (wc == null)
                    return EditorMode.Universal;

                return Player.Clan == wc ? EditorMode.Player : EditorMode.Universal;
            }
        }

        /// <summary>
        /// Opens the editor for the current clan page.
        /// </summary>
        [DataSourceMethod]
        public override void ExecuteOpenEditor()
        {
            try
            {
                if (ViewModel.Obj is not Clan clan)
                    return;

                var wc = WClan.Get(clan);
                if (wc == null)
                    return;

                if (Player.Clan == wc)
                {
                    EditorLauncher.Launch(EditorLaunchArgs.Player(faction: wc));
                    return;
                }

                // Other clans stay universal.
                EditorLauncher.Launch(EditorLaunchArgs.Universal(faction: wc));
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
