using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor;
using Retinues.Game;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Retinues.UI.Prefabs.Encyclopedia
{
    [ViewModelMixin]
    public sealed class ClanPageScreen(EncyclopediaClanPageVM vm)
        : BaseEncyclopediaPageScreen<EncyclopediaClanPageVM>(vm)
    {
        [DataSourceProperty]
        public override int MarginTop => 6;

        [DataSourceProperty]
        public override int MarginRight => 0;

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
