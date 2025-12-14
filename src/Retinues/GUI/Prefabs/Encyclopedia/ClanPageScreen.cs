using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Editor.Screen;
using Retinues.Model.Factions;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Retinues.GUI.Prefabs.Encyclopedia
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

                // Get the wrapped clan.
                var wc = WClan.Get(clan);
                if (wc == null)
                    return;

                // Launch the editor with the clan.
                EditorLauncher.Launch(wc);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
