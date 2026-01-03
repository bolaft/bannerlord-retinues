using System;
using Bannerlord.UIExtenderEx.Attributes;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Editor;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;

namespace Retinues.UI.Prefabs.Encyclopedia
{
    [ViewModelMixin]
    public sealed class FactionPageScreen(EncyclopediaFactionPageVM vm)
        : BaseEncyclopediaPageScreen<EncyclopediaFactionPageVM>(vm)
    {
        [DataSourceProperty]
        public override int MarginTop => 8;

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

                var playerKingdom = Hero.MainHero?.Clan?.Kingdom;

                if (playerKingdom != null && ReferenceEquals(playerKingdom, kingdom))
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
